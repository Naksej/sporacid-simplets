﻿namespace Sporacid.Simplets.Webapp.Services.Services.Clubs.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Http;
    using Sporacid.Simplets.Webapp.Core.Repositories;
    using Sporacid.Simplets.Webapp.Services.Database;
    using Sporacid.Simplets.Webapp.Services.Database.Dto;
    using Sporacid.Simplets.Webapp.Services.Database.Dto.Clubs;
    using Sporacid.Simplets.Webapp.Services.Database.Repositories;
    using WebApi.OutputCache.V2;

    /// <authors>Simon Turcotte-Langevin, Patrick Lavallée, Jean Bernier-Vibert</authors>
    /// <version>1.9.0</version>
    [RoutePrefix(BasePath + "/{clubName:alpha}/membre")]
    public class MembreController : BaseSecureService, IMembreService
    {
        private readonly IEntityRepository<Int32, Club> clubRepository;
        private readonly IEntityRepository<Int32, Membre> membreRepository;

        public MembreController(IEntityRepository<Int32, Membre> membreRepository, IEntityRepository<Int32, Club> clubRepository)
        {
            this.membreRepository = membreRepository;
            this.clubRepository = clubRepository;
        }

        /// <summary>
        /// Get all membre entities from a club context.
        /// </summary>
        /// <param name="clubName">The unique club name of the club entity.</param>
        /// <param name="skip">Optional parameter. Specifies how many entities to skip.</param>
        /// <param name="take">Optional parameter. Specifies how many entities to take.</param>
        /// <returns>The fournisseur entities.</returns>
        [HttpGet, Route("")]
        [CacheOutput(ServerTimeSpan = (Int32) CacheDuration.Medium)]
        public IEnumerable<WithId<Int32, MembreDto>> GetAll(String clubName, [FromUri] UInt32? skip = null, [FromUri] UInt32? take = null)
        {
            return this.membreRepository
                .GetAll(membre => membre.Club.Nom == clubName)
                .OptionalSkipTake(skip, take)
                .MapAllWithIds<Membre, MembreDto>();
        }

        /// <summary>
        /// Get all membre entities in the given group from a club context.
        /// </summary>
        /// <param name="clubName">The unique club name of the club entity.</param>
        /// <param name="groupeId">The groupe id.</param>
        /// <param name="skip">Optional parameter. Specifies how many entities to skip.</param>
        /// <param name="take">Optional parameter. Specifies how many entities to take.</param>
        /// <returns>The fournisseur entities.</returns>
        [HttpGet, Route("in/{groupeId:int}")]
        [CacheOutput(ServerTimeSpan = (Int32) CacheDuration.Medium)]
        public IEnumerable<WithId<Int32, MembreDto>> GetAllInGroupe(String clubName, Int32 groupeId, [FromUri] UInt32? skip = null, UInt32? take = null)
        {
            return this.membreRepository
                .GetAll(membre => membre.Club.Nom == clubName && membre.GroupeMembres.Any(gp => gp.GroupeId == groupeId))
                .OptionalSkipTake(skip, take)
                .MapAllWithIds<Membre, MembreDto>();
        }

        /// <summary>
        /// Get a membre entity from a club context.
        /// </summary>
        /// <param name="clubName">The unique club name of the club entity.</param>
        /// <param name="membreId">The membre id.</param>
        /// <returns>The membre entity.</returns>
        [HttpGet, Route("{membreId:int}")]
        [CacheOutput(ServerTimeSpan = (Int32) CacheDuration.Medium, ClientTimeSpan = (Int32) CacheDuration.Medium)]
        public MembreDto Get(String clubName, Int32 membreId)
        {
            return this.membreRepository
                .GetUnique(membre => membre.Club.Nom == clubName && membre.Id == membreId)
                .MapTo<Membre, MembreDto>();
        }

        /// <summary>
        /// Creates a membre in a club context.
        /// </summary>
        /// <param name="clubName">The unique club name of the club entity.</param>
        /// <param name="membre">The membre.</param>
        /// <returns>The created membre id.</returns>
        [HttpPost, Route("")]
        [InvalidateCacheOutput("GetAll"), InvalidateCacheOutput("GetAllInGroupe")]
        public int Create(String clubName, MembreDto membre)
        {
            var clubEntity = this.clubRepository.GetUnique(club => clubName == club.Nom);
            var membreEntity = membre.MapTo<MembreDto, Membre>();

            // Make sure the membre is created in this context.
            membreEntity.ClubId = clubEntity.Id;

            this.membreRepository.Add(membreEntity);
            return membreEntity.Id;
        }

        /// <summary>
        /// Udates a membre in a club context.
        /// </summary>
        /// <param name="clubName">The unique club name of the club entity.</param>
        /// <param name="membreId">The membre id.</param>
        /// <param name="membre">The membre.</param>
        [HttpPut, Route("{membreId:int}")]
        [InvalidateCacheOutput("Get"), InvalidateCacheOutput("GetAll"), InvalidateCacheOutput("GetAllInGroupe")]
        public void Update(String clubName, Int32 membreId, MembreDto membre)
        {
            var membreEntity = this.membreRepository
                .GetUnique(membre2 => membre2.Club.Nom == clubName && membre2.Id == membreId)
                .MapFrom(membre);
            this.membreRepository.Update(membreEntity);
        }

        /// <summary>
        /// Deletes a membre from a club context.
        /// </summary>
        /// <param name="clubName">The unique club name of the club entity.</param>
        /// <param name="membreId">The membre id.</param>
        [HttpDelete, Route("{membreId:int}")]
        [InvalidateCacheOutput("Get"), InvalidateCacheOutput("GetAll"), InvalidateCacheOutput("GetAllInGroupe")]
        public void Delete(String clubName, Int32 membreId)
        {
            // Somewhat trash call to make sure the membre is in this context. 
            var membreEntity = this.membreRepository
                .GetUnique(membre => clubName == membre.Club.Nom && membre.Id == membreId);
            this.membreRepository.Delete(membreEntity);
        }
    }
}