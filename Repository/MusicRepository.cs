using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Clam.Areas.Music.Models;
using Clam.Interface.Music;
using Clam.Utilities;
using ClamDataLibrary.DataAccess;
using ClamDataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace Clam.Repository.Music
{
    public class MusicRepository : Repository<ClamUserMusic>, IMusicRepository
    {
        private readonly UserManager<ClamUserAccountRegister> _userManager;
        private new readonly ClamUserAccountContext _context;
        private readonly IMapper _mapper;

        public MusicRepository(ClamUserAccountContext context, UserManager<ClamUserAccountRegister> userManager, IMapper mapper) : base(context)
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task AddAsyncGenre(AreaUserMusicCategory model)
        {
            var result = _mapper.Map<ClamUserMusicCategory>(model);
            await _context.ClamUserMusicCategories.AddAsync(result);
            await _context.SaveChangesAsync();
        }

        public void AddGenre(AreaUserMusicCategory model)
        {
            var result = _mapper.Map<ClamUserMusicCategory>(model);
            _context.ClamUserMusicCategories.Add(result);
            _context.SaveChangesAsync();
        }

        public async Task<AreaUserMusicCategory> GetAsyncGenre(Guid id)
        {
            var model = await _context.ClamUserMusicCategories.FindAsync(id);
            return _mapper.Map<AreaUserMusicCategory>(model);
        }

        public async Task<IEnumerable<MusicGenreSelection>> GetAsyncGenreWithMusicTracks(Guid id)
        {
            var songs = await _context.ClamUserMusic.ToListAsync();
            var category = await _context.ClamUserMusicCategories.FindAsync(id);
            var joinTable = await _context.ClamUserMusicJoinCategories.ToListAsync();

            List<MusicGenreSelection> model = new List<MusicGenreSelection>();
            foreach (var item in songs)
            {
                if (joinTable.Any(val => val.CategoryId == category.CategoryId && val.SongId == item.SongId))
                {
                    model.Add(new MusicGenreSelection()
                    {
                        SongId = item.SongId,
                        SongArtist = item.SongArtist,
                        SongTitle = item.SongTitle,
                        IsSelected = true
                    });
                }
                else
                {
                    model.Add(new MusicGenreSelection()
                    {
                        SongId = item.SongId,
                        SongArtist = item.SongArtist,
                        SongTitle = item.SongTitle,
                        IsSelected = false
                    });
                }
            }
            return model;
        }

        public async Task<AreaUserMusic> GetAsyncMusicTrack(Guid id)
        {
            var model = await _context.ClamUserMusic.FindAsync(id);
            return _mapper.Map<AreaUserMusic>(model);
        }

        public AreaUserMusicCategory GetGenre(Guid id)
        {
            var model = _context.ClamUserMusicCategories.Find(id);
            return _mapper.Map<AreaUserMusicCategory>(model);
        }

        public AreaUserMusic GetMusicTrack(Guid id)
        {
            var model = _context.ClamUserMusic.Find(id);
            return _mapper.Map<AreaUserMusic>(model);
        }

        public async Task<IEnumerable<AreaUserMusicCategory>> GetAllMusicGenres()
        {
            var model = await _context.ClamUserMusicCategories.ToListAsync();
            List<AreaUserMusicCategory> result = new List<AreaUserMusicCategory>();
            foreach (var item in model)
            {
                result.Add(new AreaUserMusicCategory()
                {
                    CategoryId = item.CategoryId,
                    CategoryName = item.CategoryName,
                    LastModified = item.LastModified,
                    DateCreated = item.DateCreated
                });
            }
            return result;
        }

        public async Task<IEnumerable<MusicGenreSelection>> GetAllMusicTracksForGenreSelection(Guid id, string userName)
        {
            var songs = await _context.ClamUserMusic.ToListAsync();
            var genre = await _context.ClamUserMusicCategories.FindAsync(id);
            var joinTable = await _context.ClamUserMusicJoinCategories.ToListAsync();

            var userProfile = await _userManager.FindByNameAsync(userName);

            List<MusicGenreSelection> model = new List<MusicGenreSelection>();
            foreach (var item in songs)
            {
                if ((joinTable.Any(val => val.CategoryId == genre.CategoryId && val.SongId == item.SongId))
                    && (item.UserId.Equals(userProfile.Id)))
                {
                    model.Add(new MusicGenreSelection()
                    {
                        SongId = item.SongId,
                        SongArtist = item.SongArtist,
                        SongTitle = item.SongTitle,
                        IsSelected = true
                    });
                }
                else if (!(joinTable.Any(val => val.CategoryId == genre.CategoryId && val.SongId == item.SongId))
                    && (item.UserId.Equals(userProfile.Id)))
                {
                    model.Add(new MusicGenreSelection()
                    {
                        SongId = item.SongId,
                        SongArtist = item.SongArtist,
                        SongTitle = item.SongTitle,
                        IsSelected = false
                    });
                }
            }
            return model;
        }

        public async Task<IEnumerable<AreaUserMusic>> GetAllUserMusic(string userName)
        {
            var model = await _context.ClamUserMusic.ToListAsync();
            var getUser = await _userManager.FindByNameAsync(userName);

            List<AreaUserMusic> result = new List<AreaUserMusic>();
            foreach (var item in model)
            {
                if (item.UserId == getUser.Id)
                {
                    result.Add(new AreaUserMusic()
                    {
                        SongId = item.SongId,
                        SongTitle = item.SongTitle,
                        SongArtist = item.SongArtist,
                        ItemPath = item.ItemPath,
                        Size = item.Size,
                        Status = item.Status
                    });
                }
            }
            return result;
        }

        public async Task<MusicHome> GetDisplayHomeContent(string search = null)
        {

            // Retrieve Tracks from Database
            var tracks = await _context.ClamUserMusic.ToListAsync();
            var categories = await _context.ClamUserMusicCategories.ToListAsync();
            var joinTable = await _context.ClamUserMusicJoinCategories.ToListAsync();

            // Random Number Generator
            var random = new Random();
            List<int> recommendedIndexRegister = new List<int>();

            // Restriction Level Limits on List
            int restriction_Standard = 10;
            int restriction_General = 5;
            int restriction_RecentlyAdded = tracks.Count;

            MusicHome model = new MusicHome();
            if ((tracks.Count == 0) || (categories.Count == 0) || (joinTable.Count == 0))
            {
                return model;
            }

            // Arrange Model 

            if (!String.IsNullOrEmpty(search))
            {
                foreach (var track in tracks)
                {
                    if (track.Status == true)
                    {

                        // Convert Title to lower string
                        var filterTitle = track.SongTitle.ToLower();
                        var filterArtist = track.SongArtist.ToLower();
                        var getGenreId = track.ClamUserMusicJoinCategories;

                        // Search Keyword
                        if ((filterTitle.Contains(search.ToLower())) || (filterArtist.Contains(search.ToLower()))
                            || (getGenreId.Any(x => x.ClamUserMusicCategory.CategoryName.ToLower().Contains(search.ToLower()))))
                        {
                            model.AreaUserMusics.Add(new AreaUserMusic()
                            {
                                SongTitle = track.SongTitle,
                                SongArtist = track.SongArtist,
                                SongId = track.SongId,
                                ItemPath = FilePathUrlHelper.DataFilePathFilter(track.ItemPath, 3)
                            });
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                model.SearchRequest = search;
                model.SearchRequestResultsCount = model.AreaUserMusics.Count;
                return model;
            }
            else
            {
                // For-Loop iterates through every single track and adds the track to the collect all track list
                foreach (var track in tracks)
                {
                    if (track.Status == true)
                    {
                        model.AreaUserMusics.Add(new AreaUserMusic()
                        {
                            SongTitle = track.SongTitle,
                            SongArtist = track.SongArtist,
                            SongId = track.SongId,
                            ItemPath = FilePathUrlHelper.DataFilePathFilter(track.ItemPath, 3)
                        });
                        model.RecentlyAdded.Add(new AreaUserMusic()
                        {
                            SongTitle = track.SongTitle,
                            SongArtist = track.SongArtist,
                            SongId = track.SongId,
                            ItemPath = FilePathUrlHelper.DataFilePathFilter(track.ItemPath, 3)
                        });
                    }
                }

                // For-Loop iterate through each category to retrieve particular songs
                foreach (var selectedCategory in categories)
                {
                    List<AreaUserMusic> categoryTracks = new List<AreaUserMusic>();
                    foreach (var track in tracks)
                    {
                        if (track.Status == true
                            && joinTable.Any(x => x.SongId == track.SongId && x.CategoryId == selectedCategory.CategoryId
                            && !categoryTracks.Any(x => x.SongId.Equals(track.SongId))))
                        {
                            categoryTracks.Add(new AreaUserMusic()
                            {
                                SongTitle = track.SongTitle,
                                SongArtist = track.SongArtist,
                                SongId = track.SongId,
                                ItemPath = FilePathUrlHelper.DataFilePathFilter(track.ItemPath, 3)
                            });
                        }
                    }
                    // Store each list into main model MusicHome
                    model.AreaUserMusicCategories.Add(new AreaUserMusicCategory()
                    {
                        CategoryId = selectedCategory.CategoryId,
                        CategoryName = selectedCategory.CategoryName,
                        AreaUserMusicCategories = categoryTracks

                    });
                }

                // For-Loop for Recommended Listing
                foreach (var track in tracks)
                {
                    int randomIndex = random.Next(tracks.Count);
                    int recommendedIndex = random.Next(tracks.Count);
                    if (track.Status == true)
                    {
                        if ((restriction_General != 0) && !(recommendedIndexRegister.Contains(recommendedIndex)))
                        {
                            var recommended = tracks[recommendedIndex];
                            model.RecommendedList.Add(new AreaUserMusic()
                            {
                                SongId = recommended.SongId,
                                SongArtist = recommended.SongArtist,
                                SongTitle = recommended.SongTitle,
                                ItemPath = FilePathUrlHelper.DataFilePathFilter(recommended.ItemPath, 3)
                            });
                            recommendedIndexRegister.Add(recommendedIndex);
                            restriction_General -= 1;
                        }
                        if (restriction_General == 0)
                        {
                            break;
                        }
                    }
                }
                return model;
            }
        }

        public async Task RemoveGenre(Guid id)
        {
            var model = await _context.ClamUserMusicCategories.FindAsync(id);
            _context.ClamUserMusicCategories.Remove(model);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveMusicTrack(Guid id)
        {
            var model = await _context.ClamUserMusic.FindAsync(id);
            var result = FilePathUrlHelper.DataFilePathFilterIndex(model.ItemPath, 4);
            var path = model.ItemPath.Substring(0, result);
            Directory.Delete(path, true);
            _context.ClamUserMusic.Remove(model);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveRangeMusicTracks(List<AreaUserMusic> model)
        {
            foreach (var item in model)
            {
                var result = await _context.ClamUserMusic.FindAsync(item.SongId);
                var pathFilter = FilePathUrlHelper.DataFilePathFilterIndex(result.ItemPath, 4);
                var path = result.ItemPath.Substring(0, pathFilter);
                Directory.Delete(path, true);
                _context.ClamUserMusic.Remove(result);
            }
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAllMusicTrackGenreSelection(Guid id, List<MusicGenreSelection> model)
        {
            var category = await _context.ClamUserMusicCategories.FindAsync(id);
            var queryTable = await _context.ClamUserMusicJoinCategories.AsNoTracking().ToListAsync();
            List<ClamUserMusicJoinCategory> joinTables = new List<ClamUserMusicJoinCategory>();

            foreach (var item in model)
            {
                if (item.IsSelected == true && (queryTable.Any(val => val.CategoryId == category.CategoryId && val.SongId == item.SongId)))
                {
                    continue;
                }
                if (item.IsSelected == true && !(queryTable.Any(val => val.CategoryId == category.CategoryId && val.SongId == item.SongId)))
                {
                    joinTables.Add(new ClamUserMusicJoinCategory()
                    {
                        CategoryId = category.CategoryId,
                        SongId = item.SongId
                    });
                }
                if (item.IsSelected == false && (queryTable.Any(val => val.CategoryId == category.CategoryId && val.SongId == item.SongId)))
                {
                    _context.Remove(new ClamUserMusicJoinCategory() { SongId = item.SongId, CategoryId = category.CategoryId });
                }
            }
            await _context.AddRangeAsync(joinTables);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMusicTrack(Guid id, StreamFormDataMusic formData)
        {
            var model = await _context.ClamUserMusic.FindAsync(id);
            _context.Entry(model).Entity.SongTitle = formData.SongTitle;
            _context.Entry(model).Entity.SongArtist = formData.SongArtist;
            _context.Entry(model).Entity.Status = bool.Parse(formData.Status);
            _context.Entry(model).Entity.LastModified = DateTime.Now;
            _context.Entry(model).State = EntityState.Modified;
            _context.Update(model);
            await _context.SaveChangesAsync();
        }
    }
}
