﻿namespace Sporacid.Simplets.Webapp.Services.Database.Dto.Clubs
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <authors>Simon Turcotte-Langevin, Patrick Lavallée, Jean Bernier-Vibert</authors>
    /// <version>1.9.0</version>
    [Serializable]
    public class MembreDto
    {
        [Required]
        [StringLength(50)]
        public String Titre { get; set; }

        [Required]
        public DateTime DateDebut { get; set; }

        public DateTime DateFin { get; set; }

        [Required]
        public Boolean Actif { get; set; }
    }
}