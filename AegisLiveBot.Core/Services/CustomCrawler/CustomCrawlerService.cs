using AegisLiveBot.Core.Common;
using AegisLiveBot.DAL.Models.CustomCrawler;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AegisLiveBot.Core.Services.CustomCrawler
{
    public interface ICustomCrawlerService : IStartUpService
    {
        void SetUpCustomReplyEditor(DiscordChannel channel, ulong userId);
    }
    public class CustomCrawlerService : ICustomCrawlerService
    {
        private readonly DbService _db;
        private readonly DiscordClient _client;

        private List<CustomReply> CustomReplies;

        private List<ulong> ActiveEditorsInChannels = new List<ulong>();

        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public CustomCrawlerService(DbService db, DiscordClient client)
        {
            _db = db;
            _client = client;

            Task.Run(async () => await Crawl().ConfigureAwait(false));
        }

        private async Task Crawl()
        {
            SetUpCustomReplies();

            while (true)
            {
                var interactivity = _client.GetInteractivity();
                var response = await interactivity.WaitForMessageAsync(x => x.Author.Id != _client.CurrentUser.Id).ConfigureAwait(false);
                if (response.TimedOut)
                {
                    continue;
                }

                var uow = _db.UnitOfWork();
                var customReplyMode = uow.ServerSettings.GetOrAddByGuildId(response.Result.Channel.GuildId).CustomReplyMode;
                if (!customReplyMode)
                {
                    continue;
                }

                var customReplies = CustomReplies.Where(x => x.GuildId == response.Result.Channel.GuildId // same server
                    && (x.Channels.Count == 0 || x.Channels.Select(x => x.Id).Contains(response.Result.ChannelId)) // no channels selected, or selected channels only
                    && DateTime.UtcNow.AddMinutes(-x.Cooldown) > x.LastTriggered // cooldown
                    && x.Triggers.Any(y => y.All(z => response.Result.Content.IndexOf(z) >= 0))); // any triggers hit

                if(customReplies == null || customReplies.Count() == 0)
                {
                    continue;
                }

                var reply = customReplies.FirstOrDefault();
                await response.Result.Channel.SendMessageAsync(reply.Message).ConfigureAwait(false);
                reply.LastTriggered = DateTime.UtcNow;
            }
        }

        private void SetUpCustomReplies()
        {
            CustomReplies = new List<CustomReply>();

            var uow = _db.UnitOfWork();

            var customReplies = uow.CustomReplies.GetAll();

            foreach(var customReplyDb in customReplies)
            {
                try
                {
                    var channels = customReplyDb.ChannelIds.Split(',').Select(x => ulong.Parse(x.Trim())).Distinct().Select(x => _client.GetChannelAsync(x).Result).ToList();
                    var triggers = customReplyDb.Triggers.Split(';').Select(x => x.Split(',').Select(y => y.ToLower()).ToList()).ToList();
                    var customReply = new CustomReply
                    {
                        Id = customReplyDb.Id,
                        GuildId = customReplyDb.GuildId,
                        Channels = channels,
                        Message = customReplyDb.Message,
                        Triggers = triggers,
                        Cooldown = customReplyDb.Cooldown
                    };
                    CustomReplies.Add(customReply);
                }
                catch (Exception e)
                {
                    AegisLog.Log($"Error adding customReply: {JsonConvert.SerializeObject(customReplyDb)}", e);
                }
            }
        }

        public void SetUpCustomReplyEditor(DiscordChannel channel, ulong userId)
        {
            Task.Run(async () =>
            {
                if (ActiveEditorsInChannels.Contains(channel.Id))
                {
                    await channel.SendMessageAsync("An editor is already active in this channel.").ConfigureAwait(false);
                    return;
                } else
                {
                    ActiveEditorsInChannels.Add(channel.Id);
                }

                try
                {
                    var interactivity = _client.GetInteractivity();

                    await EditorList(interactivity, channel, userId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await channel.SendMessageAsync($"Error occurred within editor: {e.Message}").ConfigureAwait(false);
                    AegisLog.Log(e.Message, e);
                }
                finally
                {
                    await channel.SendMessageAsync("Custom reply editor exited.").ConfigureAwait(false);
                    ActiveEditorsInChannels.Remove(channel.Id);
                }
            });
        }

        // returns true if the editor should exit completely
        private async Task<bool> EditorAddOrUpdate(InteractivityExtension interactivity, DiscordChannel channel, ulong userId, bool editMode = false, CustomReply currentCustomReply = null)
        {
            var customReply = currentCustomReply ?? new CustomReply
            {
                GuildId = channel.GuildId,
                Message = "",
                Triggers = new List<List<string>>(),
                Channels = new List<DiscordChannel>(),
                Cooldown = 5
            };

            var helpMsg = "```Use the following commands(without prefix) to edit a new custom reply:\n";
            helpMsg += "setmessage <message>\n";
            helpMsg += "-- For example: setmessage Crit Fiora sucks\n\n";
            helpMsg += "addtrigger <triggers>\n";
            helpMsg += "-- Add multiple words to each trigger by separating them with commas\n";
            helpMsg += "-- A message needs to contain all the words within each trigger to activate\n";
            helpMsg += "-- For example: addtrigger fiora,crit - will trigger on messages with both 'fiora' and 'crit' anywhere\n";
            helpMsg += "-- For example: addtrigger fiora crit - will trigger on messages with 'fiora crit' anywhere\n";
            helpMsg += "-- The same message can have multiple triggers that share the same cooldown\n\n";
            helpMsg += "removetrigger <number>\n";
            helpMsg += "-- Removes a trigger at number, use 'preview' to see current triggers\n";
            helpMsg += "-- For example: removetrigger 1\n\n";
            helpMsg += "addchannel <channel>\n";
            helpMsg += "-- Add the channel that triggers this message\n";
            helpMsg += "-- For example: addchannel #general\n\n";
            helpMsg += "removechannel <channel>\n";
            helpMsg += "-- Removes a channel, use 'preview' to see current channels\n";
            helpMsg += "-- For example: removechannel #general\n\n";
            helpMsg += "setcooldown <minutes>\n";
            helpMsg += "-- Sets the cooldown for the custom reply in minutes\n\n";
            helpMsg += "===========================================\n";
            helpMsg += "Use the following commands to navigate the editor:\n";
            helpMsg += "help - display this message\n";
            helpMsg += "preview - preview the current custom reply\n";
            helpMsg += "save - saves this trigger\n";
            helpMsg += "back - return to menu\n";
            helpMsg += "quit - quit editor without saving\n";
            helpMsg += "```";

            var noChannelWarningMsg = "```WARNING: NO CHANNELS ADDED, CUSTOM REPLY WILL TRIGGER IN ALL CHANNELS\n";
            noChannelWarningMsg += "TO CONFIGURE THE CUSTOM REPLY TO TRIGGER IN SPECIFIC CHANNELS, USE 'addchannel <channels>'```";

            await channel.SendMessageAsync(helpMsg).ConfigureAwait(false);

            while (true)
            {
                var response = await interactivity.WaitForMessageAsync(x => x.ChannelId == channel.Id && x.Author.Id == userId).ConfigureAwait(false);
                if (response.TimedOut)
                {
                    await channel.SendMessageAsync("Inactivity: editor will now exit.").ConfigureAwait(false);
                    return true;
                }

                var responseMsg = response.Result.Content;
                var command = responseMsg.Split(' ')[0].ToLower();
                var argument = "";
                var channelsArgument = response.Result.MentionedChannels;
                if (responseMsg.Length > command.Length + 1)
                {
                    argument = responseMsg.Substring(command.Length + 1);
                }

                // everything here has no argument
                if (command == "help")
                {
                    await channel.SendMessageAsync(helpMsg).ConfigureAwait(false);
                }
                else if(command == "preview")
                {
                    await channel.SendMessageAsync($"{CustomReplyHelper.ToString(customReply)}").ConfigureAwait(false);
                }
                else if(command == "save")
                {
                    if (string.IsNullOrEmpty(customReply.Message))
                    {
                        await channel.SendMessageAsync($"Custom reply message cannot be empty. Use 'setmessage <message>' to set a message.").ConfigureAwait(false);
                    }
                    else if (customReply.Triggers.Count == 0)
                    {
                        await channel.SendMessageAsync($"Add at least one trigger for the custom reply. Use 'addtrigger <trigger words>' to add triggers.").ConfigureAwait(false);
                    }
                    else
                    {
                        if (customReply.Channels.Count == 0)
                        {
                            await channel.SendMessageAsync(noChannelWarningMsg).ConfigureAwait(false);
                        }

                        await channel.SendMessageAsync(CustomReplyHelper.ToString(customReply)).ConfigureAwait(false);
                        await channel.SendMessageAsync($"Confirm custom reply by typing 'confirm', anything else will cancel this request.").ConfigureAwait(false);

                        response = await interactivity.WaitForMessageAsync(x => x.ChannelId == channel.Id && x.Author.Id == userId).ConfigureAwait(false);
                        if (response.TimedOut)
                        {
                            await channel.SendMessageAsync("Inactivity: editor will now exit.").ConfigureAwait(false);
                            return true;
                        }

                        if (response.Result.Content.ToLower() == "confirm")
                        {
                            await semaphoreSlim.WaitAsync();
                            try
                            {
                                if (editMode)
                                {
                                    CustomReplies[CustomReplies.FindIndex(x => x.Id == customReply.Id)] = customReply;

                                    var uow = _db.UnitOfWork();
                                    uow.CustomReplies.UpdateByGuildId(channel.GuildId, customReply);
                                    await uow.SaveAsync().ConfigureAwait(false);

                                }
                                else
                                {
                                    var uow = _db.UnitOfWork();
                                    var customReplyDb = uow.CustomReplies.AddByGuildId(channel.GuildId, customReply);
                                    await uow.SaveAsync().ConfigureAwait(false);

                                    customReply.Id = customReplyDb.Id;
                                    CustomReplies.Add(customReply);
                                }

                                await channel.SendMessageAsync("Custom reply successfully saved.").ConfigureAwait(false);
                            }
                            catch (Exception e)
                            {
                                throw;
                            }
                            finally
                            {
                                semaphoreSlim.Release();
                            }
                        }
                        else
                        {
                            await channel.SendMessageAsync("Save request cancelled.").ConfigureAwait(false);
                            continue;
                        }

                        return false;
                    }
                }
                else if(command == "back")
                {
                    return false;
                }
                else if(command == "quit")
                {
                    break;
                }
                else
                {
                    // everything from here has at least 1 argument
                    if (command == "setmessage")
                    {
                        if (string.IsNullOrEmpty(argument))
                        {
                            await channel.SendMessageAsync("Please enter an argument.").ConfigureAwait(false);
                            break;
                        }

                        customReply.Message = argument;
                        await channel.SendMessageAsync($"Custom reply message set to: {argument}").ConfigureAwait(false);
                    }
                    else if (command == "addtrigger")
                    {
                        if (string.IsNullOrEmpty(argument))
                        {
                            await channel.SendMessageAsync("Please enter an argument.").ConfigureAwait(false);
                            break;
                        }

                        var words = argument.Split(",").Select(x => x.Trim()).ToList();
                        customReply.Triggers.Add(words);
                        await channel.SendMessageAsync($"Custom reply set to trigger for messages containing: {string.Join(", ", words)}").ConfigureAwait(false);
                    }
                    else if (command == "removetrigger")
                    {
                        if (string.IsNullOrEmpty(argument))
                        {
                            await channel.SendMessageAsync("Please enter an argument.").ConfigureAwait(false);
                            break;
                        }

                        if (int.TryParse(argument, out int result))
                        {
                            if (result - 1 >= customReply.Triggers.Count)
                            {
                                await channel.SendMessageAsync($"Number out of range.").ConfigureAwait(false);
                            } else
                            {
                                var words = customReply.Triggers[result - 1];
                                customReply.Triggers.RemoveAt(result - 1);
                                await channel.SendMessageAsync($"Removed trigger for custom reply: {string.Join(", ", words)}").ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await channel.SendMessageAsync("Please enter a valid number.").ConfigureAwait(false);
                        }
                    }
                    else if (command == "addchannel")
                    {
                        if (string.IsNullOrEmpty(argument))
                        {
                            await channel.SendMessageAsync("Please enter an argument.").ConfigureAwait(false);
                            break;
                        }

                        if (channelsArgument.Count == 0)
                        {
                            await channel.SendMessageAsync("Please mention at least 1 channel.").ConfigureAwait(false);
                        }
                        else
                        {
                            foreach (var channelArgument in channelsArgument)
                            {
                                customReply.Channels.Add(channelArgument);
                            }
                            await channel.SendMessageAsync($"Custom reply set to trigger in these channels: {string.Join(" ", channelsArgument.Select(x => x.Mention))}").ConfigureAwait(false);
                        }
                    }
                    else if (command == "removechannel")
                    {
                        if (string.IsNullOrEmpty(argument))
                        {
                            await channel.SendMessageAsync("Please enter an argument.").ConfigureAwait(false);
                            break;
                        }

                        if (channelsArgument.Count == 0)
                        {
                            await channel.SendMessageAsync("Please mention at least 1 channel.").ConfigureAwait(false);
                        }
                        else
                        {
                            foreach (var channelArgument in channelsArgument)
                            {
                                customReply.Channels.Remove(channelArgument);
                            }
                            await channel.SendMessageAsync($"Removed trigger for custom reply in these channels: {string.Join(" ", channelsArgument.Select(x => x.Mention))}").ConfigureAwait(false);
                        }
                    }
                    else if (command == "setcooldown")
                    {
                        if (string.IsNullOrEmpty(argument))
                        {
                            await channel.SendMessageAsync("Please enter an argument.").ConfigureAwait(false);
                            break;
                        }

                        if (int.TryParse(argument, out int result))
                        {
                            customReply.Cooldown = result;
                            await channel.SendMessageAsync($"Custom reply cooldown set to {result} minutes.").ConfigureAwait(false);
                        } else
                        {
                            await channel.SendMessageAsync("Please enter a valid number.").ConfigureAwait(false);
                        }
                    }
                }
            }
            return true;
        }

        private async Task<bool> EditorList(InteractivityExtension interactivity, DiscordChannel channel, ulong userId)
        {
            var helpMsg = "```Use the following commands(without prefix) to view, add, or edit existing custom replies:\n";
            helpMsg += "view <number>\n";
            helpMsg += "-- View the specified custom reply\n";
            helpMsg += "-- For example: view 1\n\n";
            helpMsg += "viewpage\n";
            helpMsg += "-- View current page\n\n";
            helpMsg += "nextpage\n";
            helpMsg += "-- Go to next page\n\n";
            helpMsg += "prevpage\n";
            helpMsg += "-- Go to previous page\n\n";
            helpMsg += "gotopage <number>\n";
            helpMsg += "-- Go to specified page number\n";
            helpMsg += "-- For example: gotopage 2\n\n";
            helpMsg += "add\n";
            helpMsg += "-- Add a new custom reply\n\n";
            helpMsg += "edit <number>\n";
            helpMsg += "-- Edits the specified custom reply\n";
            helpMsg += "-- For example: edit 1\n\n";
            helpMsg += "remove <number>\n";
            helpMsg += "-- Removes the specified custom reply\n";
            helpMsg += "-- For example: remove 1\n\n";
            helpMsg += "togglecustomreply\n";
            helpMsg += "-- Toggles custom reply mode on and off for this server\n\n";
            helpMsg += "===========================================\n";
            helpMsg += "Use the following commands to navigate the editor:\n";
            helpMsg += "help - display this message\n";
            helpMsg += "back - return to menu\n";
            helpMsg += "quit - quit editor\n";
            helpMsg += "```";
            await channel.SendMessageAsync(helpMsg).ConfigureAwait(false);

            var warningMsg = "```WARNING: CUSTOM REPLY MODE IS CURRENTLY OFF FOR THIS SERVER\n";
            warningMsg += "USE THE COMMAND 'togglecustomreply' TO TURN ON CUSTOM REPLY```";

            var serverCustomReplies = CustomReplies.Where(x => x.GuildId == channel.GuildId).ToList();
            var currentPage = 1;
            var maxPage = (serverCustomReplies.Count / 10) + 1;
            var viewPage = true;

            while (true)
            {
                if (viewPage)
                {
                    viewPage = false;
                    var viewString = BuildPreviewPage(currentPage, maxPage, serverCustomReplies);
                    await channel.SendMessageAsync(viewString).ConfigureAwait(false);

                    var uow = _db.UnitOfWork();
                    var serverSettings = uow.ServerSettings.GetOrAddByGuildId(channel.GuildId);
                    if (!serverSettings.CustomReplyMode)
                    {
                        await channel.SendMessageAsync(warningMsg).ConfigureAwait(false);
                    }
                }

                var response = await interactivity.WaitForMessageAsync(x => x.ChannelId == channel.Id && x.Author.Id == userId).ConfigureAwait(false);
                if (response.TimedOut)
                {
                    await channel.SendMessageAsync("Inactivity: editor will now exit.").ConfigureAwait(false);
                    return true;
                }

                var responseMsg = response.Result.Content;
                var command = responseMsg.Split(' ')[0].ToLower();
                var argument = "";
                if (responseMsg.Length > command.Length + 1)
                {
                    argument = responseMsg.Substring(command.Length + 1);
                }
                var validNumber = int.TryParse(argument, out int result);

                // everything here has no argument
                if (command == "help")
                {
                    await channel.SendMessageAsync(helpMsg).ConfigureAwait(false);
                }
                else if (command == "back")
                {
                    return false;
                }
                else if (command == "quit")
                {
                    break;
                }
                else if (command == "viewpage")
                {
                    viewPage = true;
                }
                else if (command == "nextpage")
                {
                    if (currentPage == maxPage)
                    {
                        await channel.SendMessageAsync("You are already on the last page.").ConfigureAwait(false);
                    }
                    else
                    {
                        ++currentPage;
                        viewPage = true;
                    }
                }
                else if (command == "prevpage")
                {
                    if (currentPage == 1)
                    {
                        await channel.SendMessageAsync("You are already on the first page.").ConfigureAwait(false);
                    } else
                    {
                        --currentPage;
                        viewPage = true;
                    }
                }
                else if (command == "add")
                {
                    var exitEditor = await EditorAddOrUpdate(interactivity, channel, userId).ConfigureAwait(false);
                    if (exitEditor)
                    {
                        break;
                    }
                    serverCustomReplies = CustomReplies.Where(x => x.GuildId == channel.GuildId).ToList();
                    await channel.SendMessageAsync(helpMsg).ConfigureAwait(false);
                }
                else if (command == "togglecustomreply")
                {
                    var uow = _db.UnitOfWork();
                    var customReplyMode = uow.ServerSettings.ToggleCustomReply(channel.GuildId);
                    await uow.SaveAsync().ConfigureAwait(false);
                    var toggleMsg = customReplyMode ? "on" : "off";
                    await channel.SendMessageAsync($"Custom reply mode is now {toggleMsg} for this server").ConfigureAwait(false);
                }
                else
                {
                    // everything from here has at least 1 argument

                    if (command == "view")
                    {
                        if (!validNumber)
                        {
                            await channel.SendMessageAsync("Please enter a valid number.").ConfigureAwait(false);
                            continue;
                        }

                        if ((currentPage - 1) * 10 + result - 1 >= serverCustomReplies.Count || result > 10 || result <= 0)
                        {
                            await channel.SendMessageAsync($"Number out of range.").ConfigureAwait(false);
                        } else
                        {
                            await channel.SendMessageAsync(CustomReplyHelper.ToString(serverCustomReplies[(currentPage - 1) * 10 + result - 1])).ConfigureAwait(false);
                        }
                    }
                    else if (command == "gotopage")
                    {
                        if (!validNumber)
                        {
                            await channel.SendMessageAsync("Please enter a valid number.").ConfigureAwait(false);
                            continue;
                        }

                        if (result > maxPage || result <= 0)
                        {
                            await channel.SendMessageAsync($"Number out of range.").ConfigureAwait(false);
                        } else
                        {
                            currentPage = result;
                            viewPage = true;
                        }
                    }
                    else if (command == "edit")
                    {
                        if (!validNumber)
                        {
                            await channel.SendMessageAsync("Please enter a valid number.").ConfigureAwait(false);
                            continue;
                        }

                        if ((currentPage - 1) * 10 + result - 1 >= serverCustomReplies.Count || result > 10 || result <= 0)
                        {
                            await channel.SendMessageAsync($"Number out of range.").ConfigureAwait(false);
                        }
                        else
                        {
                            var customReplyToEdit = serverCustomReplies[(currentPage - 1) * 10 + result - 1];
                            var exitEditor = await EditorAddOrUpdate(interactivity, channel, userId, true, customReplyToEdit).ConfigureAwait(false);
                            if (exitEditor)
                            {
                                break;
                            }
                            serverCustomReplies = CustomReplies.Where(x => x.GuildId == channel.GuildId).ToList();
                            await channel.SendMessageAsync(helpMsg).ConfigureAwait(false);
                        }
                    }
                    else if (command == "remove")
                    {
                        if (!validNumber)
                        {
                            await channel.SendMessageAsync("Please enter a valid number.").ConfigureAwait(false);
                            continue;
                        }

                        if ((currentPage - 1) * 10 + result - 1 >= serverCustomReplies.Count || result > 10 || result <= 0)
                        {
                            await channel.SendMessageAsync($"Number out of range.").ConfigureAwait(false);
                        }
                        else
                        {
                            var customReplyToDelete = serverCustomReplies[(currentPage - 1) * 10 + result - 1];
                            await channel.SendMessageAsync(CustomReplyHelper.ToString(customReplyToDelete)).ConfigureAwait(false);
                            await channel.SendMessageAsync($"Confirm deletion by typing 'confirm', anything else will cancel this request.").ConfigureAwait(false);

                            response = await interactivity.WaitForMessageAsync(x => x.ChannelId == channel.Id && x.Author.Id == userId).ConfigureAwait(false);
                            if (response.TimedOut)
                            {
                                await channel.SendMessageAsync("Inactivity: editor will now exit.").ConfigureAwait(false);
                                return true;
                            }

                            if (response.Result.Content.ToLower() == "confirm")
                            {
                                await semaphoreSlim.WaitAsync();
                                try
                                {
                                    serverCustomReplies.Remove(customReplyToDelete);
                                    CustomReplies.Remove(customReplyToDelete);

                                    var uow = _db.UnitOfWork();
                                    uow.CustomReplies.RemoveById(customReplyToDelete.Id);
                                    await uow.SaveAsync().ConfigureAwait(false);

                                    await channel.SendMessageAsync("Custom reply deleted.").ConfigureAwait(false);
                                }
                                catch (Exception e)
                                {
                                    throw;
                                }
                                finally
                                {
                                    semaphoreSlim.Release();
                                }

                            }
                            else
                            {
                                await channel.SendMessageAsync("Deletion request cancelled.").ConfigureAwait(false);
                            }
                        }
                    }
                }
            }

            return true;
        }

        private string BuildPreviewPage(int currentPage, int maxPage, List<CustomReply> customReplies)
        {
            var msg = $"```Viewing page {currentPage} out of {maxPage}\n\n";
            if (customReplies.Count == 0)
            {
                msg += "The server currently does not have any custom replies.\n";
            }
            for (var i = 0; i + (currentPage - 1) * 10 < customReplies.Count && i < 10; ++i)
            {
                msg += $"{i + 1}. {customReplies[(currentPage - 1) * 10 + i].Message}\n";
            }
            msg += $"\nViewing page {currentPage} out of {maxPage}```";
            return msg;
        }

        internal enum EditorMode
        {
            Add,
            List,
            Unassigned
        }
    }
}
