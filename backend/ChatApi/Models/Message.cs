using System;
using System.ComponentModel.DataAnnotations;

namespace ChatApi.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }
        public string? Nickname { get; set; }  // rumuz
        public string? Text { get; set; }
        public DateTime CreatedAt { get; set; }

        // AI sonuçları
        public string? Sentiment { get; set; }   // "positive"|"neutral"|"negative"
        public double SentimentScore { get; set; }
    }
}
