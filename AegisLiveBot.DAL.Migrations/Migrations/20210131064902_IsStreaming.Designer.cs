﻿// <auto-generated />
using AegisLiveBot.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AegisLiveBot.DAL.Migrations.Migrations
{
    [DbContext(typeof(Context))]
    [Migration("20210131064902_IsStreaming")]
    partial class IsStreaming
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.3");

            modelBuilder.Entity("AegisLiveBot.DAL.Models.Fun.RoastMsg", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Msg")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("RoastMsgs");
                });

            modelBuilder.Entity("AegisLiveBot.DAL.Models.ServerSetting", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("GamesCategory")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("PriorityMode")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("RoleId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("TwitchAlertMode")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("TwitchChannelId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("ServerSettings");
                });

            modelBuilder.Entity("AegisLiveBot.DAL.Models.Streaming.LiveUserDb", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsStreaming")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("PriorityUser")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("TwitchAlert")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TwitchName")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("LiveUsers");
                });
#pragma warning restore 612, 618
        }
    }
}
