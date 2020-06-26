using Clam.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Clam.Areas.Music.Models
{
    public class AreaUserMusic
    {
        public AreaUserMusic()
        {
            AreaUserMusicJoinCategories = new HashSet<AreaUserMusicJoinCategory>();
        }

        [Key]
        [Required]
        [Display(Name = "Song ID")]
        public Guid SongId { get; set; }

        [Required]
        [MaxLength(300)]
        [DataType(DataType.Text)]
        [Display(Name = "Song (Path)")]
        public string ItemPath { get; set; }

        [Required]
        [MaxLength(30)]
        [DataType(DataType.Text)]
        [Display(Name = "Song Title")]
        public string SongTitle { get; set; }

        [Required]
        [MaxLength(30)]
        [DataType(DataType.Text)]
        [Display(Name = "Song Artist")]
        public string SongArtist { get; set; }

        [Display(Name = "Size (bytes)")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public long Size { get; set; }

        [Required]
        [MaxLength(30)]
        [Display(Name = "Viewing Status")]
        public bool Status { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Last Modified")]
        public DateTime LastModified { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date Added")]
        public DateTime DateCreated { get; set; }

        [Display(Name = "User ID")]
        public Guid UserId { get; set; }
        public virtual UserAccountRegister User { get; set; }

        public ICollection<AreaUserMusicJoinCategory> AreaUserMusicJoinCategories { get; set; }

    }

    public class AreaUserMusicCategory
    {

        public AreaUserMusicCategory()
        {
            AreaUserMusicJoinCategories = new HashSet<AreaUserMusicJoinCategory>();
            AreaUserMusicCategories = new List<AreaUserMusic>();
        }


        [Required]
        [Display(Name = "Category ID")]
        public Guid CategoryId { get; set; }

        [Required]
        [MaxLength(30)]
        [DataType(DataType.Text)]
        [Display(Name = "Genre")]
        public string CategoryName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Last Modified")]
        public DateTime LastModified { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date Added")]
        public DateTime DateCreated { get; set; }

        public List<AreaUserMusic> AreaUserMusicCategories { get; set; }

        public ICollection<AreaUserMusicJoinCategory> AreaUserMusicJoinCategories { get; set; }

    }

    public class AreaUserMusicJoinCategory
    {
        public Guid SongId { get; set; }
        public AreaUserMusic AreaUserMusic { get; set; }

        public Guid CategoryId { get; set; }
        public AreaUserMusicCategory AreaUserMusicCategory { get; set; }
    }

    public class StreamMusicDataUpload
    {
        [Key]
        [Required]
        [Display(Name = "Song ID")]
        public Guid SongId { get; set; }

        [Required]
        [MaxLength(300)]
        [DataType(DataType.Text)]
        [Display(Name = "Song (File)")]
        public IFormFile ItemPath { get; set; }

        [Required]
        [MaxLength(30)]
        [DataType(DataType.Text)]
        [Display(Name = "Song Title")]
        public string SongTitle { get; set; }

        [Required]
        [MaxLength(30)]
        [DataType(DataType.Text)]
        [Display(Name = "Song Artist")]
        public string SongArtist { get; set; }

        [Required]
        [MaxLength(30)]
        [Display(Name = "Viewing Status")]
        public bool Status { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Last Modified")]
        public DateTime LastModified { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date Added")]
        public DateTime DateCreated { get; set; }

        [Display(Name = "User ID")]
        public Guid UserId { get; set; }
        public virtual UserAccountRegister User { get; set; }
    }

    public class MusicGenreSelection
    {
        [Display(Name = "Song ID")]
        public Guid SongId { get; set; }

        [Required]
        [MaxLength(30)]
        [DataType(DataType.Text)]
        [Display(Name = "Song Title")]
        public string SongTitle { get; set; }

        [Required]
        [MaxLength(30)]
        [DataType(DataType.Text)]
        [Display(Name = "Song Artist")]
        public string SongArtist { get; set; }

        [MaxLength(30)]
        [Display(Name = "Viewing Status")]
        public string Status { get; set; }

        [Display(Name = "Select")]
        public bool IsSelected { get; set; }

        [Display(Name = "User ID")]
        public Guid UserId { get; set; }
        public virtual UserAccountRegister User { get; set; }
    }

    public class StreamFormDataMusic
    {
        [Required]
        [MaxLength(30)]
        [DataType(DataType.Text)]
        [Display(Name = "Song Title")]
        public string SongTitle { get; set; }

        [Required]
        [MaxLength(30)]
        [DataType(DataType.Text)]
        [Display(Name = "Song Artist")]
        public string SongArtist { get; set; }

        [Required]
        [MaxLength(30)]
        [Display(Name = "Viewing Status")]
        public string Status { get; set; }
    }

    public class MusicHome
    {
        public MusicHome()
        {
            AreaUserMusics = new List<AreaUserMusic>();
            AreaUserMusicCategories = new List<AreaUserMusicCategory>();
            AreaUserMusicJoinCategories = new List<AreaUserMusicJoinCategory>();
            RecentlyAdded = new List<AreaUserMusic>();
            RecommendedList = new List<AreaUserMusic>();
        }

        public List<AreaUserMusic> AreaUserMusics { get; set; }

        public List<AreaUserMusicCategory> AreaUserMusicCategories { get; set; }

        public List<AreaUserMusicJoinCategory> AreaUserMusicJoinCategories { get; set; }

        public List<AreaUserMusic> RecentlyAdded { get; set; }

        public List<AreaUserMusic> RecommendedList { get; set; }


        [Display(Name = "Searched For")]
        public string SearchRequest { get; set; }

        public int SearchRequestResultsCount { get; set; }
    }
}
