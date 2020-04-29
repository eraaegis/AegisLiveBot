using System.ComponentModel.DataAnnotations;

namespace AegisLiveBot.DAL
{
    public abstract class Entity
    {
        [Key]
        public int Id { get; set; }
    }
}
