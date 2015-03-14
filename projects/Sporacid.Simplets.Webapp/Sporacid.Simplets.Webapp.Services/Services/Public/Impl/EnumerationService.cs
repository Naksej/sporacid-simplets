﻿namespace Sporacid.Simplets.Webapp.Services.Services.Public.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Web.Http;
    using Sporacid.Simplets.Webapp.Core.Repositories;
    using Sporacid.Simplets.Webapp.Services.Database;
    using Sporacid.Simplets.Webapp.Services.Database.Dto;
    using Sporacid.Simplets.Webapp.Services.Database.Dto.Clubs;
    using Sporacid.Simplets.Webapp.Services.Database.Dto.Dbo;
    using WebApi.OutputCache.V2;

    /// <authors>Simon Turcotte-Langevin, Patrick Lavallée, Jean Bernier-Vibert</authors>
    /// <version>1.9.0</version>
    [RoutePrefix(BasePath + "/enumeration")]
    public class EnumerationService : BaseService, IEnumerationService
    {
        private readonly IRepository<Int32, Concentration> concentrationRepository;
        private readonly IRepository<Int32, StatutSuivie> statutSuivieRepository;
        private readonly IRepository<Int32, TypeContact> typeContactRepository;
        private readonly IRepository<Int32, Unite> uniteRepository;

        public EnumerationService(IRepository<Int32, TypeContact> typeContactRepository, IRepository<Int32, Concentration> concentrationRepository,
            IRepository<Int32, StatutSuivie> statutSuivieRepository, IRepository<Int32, Unite> uniteRepository)
        {
            this.typeContactRepository = typeContactRepository;
            this.concentrationRepository = concentrationRepository;
            this.statutSuivieRepository = statutSuivieRepository;
            this.uniteRepository = uniteRepository;
        }

        /// <summary>
        /// Returns all type contact entities from the system.
        /// </summary>
        /// <returns>Enumeration of all type contact entities.</returns>
        [HttpGet, Route("types-contacts")]
        [CacheOutput(ServerTimeSpan = (Int32)CacheDuration.Maximum, ClientTimeSpan = (Int32)CacheDuration.Maximum)]
        public IEnumerable<WithId<Int32, TypeContactDto>> GetAllTypesContacts()
        {
            return this.typeContactRepository
                .GetAll()
                .MapAllWithIds<TypeContact, TypeContactDto>();
        }

        /// <summary>
        /// Returns all statut suivie entities from the system.
        /// </summary>
        /// <returns>Enumeration of all statuts suivie entities.</returns>
        [HttpGet, Route("statuts-suivies")]
        [CacheOutput(ServerTimeSpan = (Int32)CacheDuration.Maximum, ClientTimeSpan = (Int32)CacheDuration.Maximum)]
        public IEnumerable<WithId<Int32, StatutSuivieDto>> GetAllStatutsSuivie()
        {
            return this.statutSuivieRepository
                .GetAll()
                .MapAllWithIds<StatutSuivie, StatutSuivieDto>();
        }

        /// <summary>
        /// Returns all concentration entities from the system.
        /// </summary>
        /// <returns>Enumeration of all concentration entities.</returns>
        [HttpGet, Route("concentrations")]
        [CacheOutput(ServerTimeSpan = (Int32)CacheDuration.Maximum, ClientTimeSpan = (Int32)CacheDuration.Maximum)]
        public IEnumerable<WithId<Int32, ConcentrationDto>> GetAllConcentrations()
        {
            return this.concentrationRepository
                .GetAll()
                .MapAllWithIds<Concentration, ConcentrationDto>();
        }

        /// <summary>
        /// Returns all unite entities from the system.
        /// </summary>
        /// <returns>Enumeration of all unite entities.</returns>
        [HttpGet, Route("unites")]
        [CacheOutput(ServerTimeSpan = (Int32)CacheDuration.Maximum, ClientTimeSpan = (Int32)CacheDuration.Maximum)]
        public IEnumerable<WithId<Int32, UniteDto>> GetAllUnites()
        {
            return this.uniteRepository
                .GetAll()
                .MapAllWithIds<Unite, UniteDto>();
        }
    }
}