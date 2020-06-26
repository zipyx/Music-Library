using Clam.Areas.Music.Models;
using ClamDataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clam.Interface.Music
{
    public interface IMusicRepository : IRepository<ClamUserMusic>
    {
        // Music Track Interface Methods
        Task<IEnumerable<AreaUserMusic>> GetAllUserMusic(string userName);
        AreaUserMusic GetMusicTrack(Guid id);
        Task<AreaUserMusic> GetAsyncMusicTrack(Guid id);
        Task<IEnumerable<MusicGenreSelection>> GetAllMusicTracksForGenreSelection(Guid id, string userName);
        Task UpdateAllMusicTrackGenreSelection(Guid id, List<MusicGenreSelection> model);
        Task UpdateMusicTrack(Guid id, StreamFormDataMusic formData);
        Task RemoveMusicTrack(Guid id);
        Task RemoveRangeMusicTracks(List<AreaUserMusic> model);


        // Music Genre
        Task<IEnumerable<AreaUserMusicCategory>> GetAllMusicGenres();
        void AddGenre(AreaUserMusicCategory model);
        Task AddAsyncGenre(AreaUserMusicCategory model);
        Task RemoveGenre(Guid id);
        AreaUserMusicCategory GetGenre(Guid id);
        Task<AreaUserMusicCategory> GetAsyncGenre(Guid id);
        Task<IEnumerable<MusicGenreSelection>> GetAsyncGenreWithMusicTracks(Guid id);

        // Home View
        Task<MusicHome> GetDisplayHomeContent(string search = null);
    }
}
