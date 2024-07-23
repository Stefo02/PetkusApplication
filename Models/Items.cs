﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetkusApplication.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Opis { get; set; }
        public string Proizvodjac { get; set; }
        public string Fabricki_kod { get; set; }
        public int Kolicina { get; set; }
        public decimal Puna_cena { get; set; }
        public string Dimenzije { get; set; }
        public decimal Tezina { get; set; }
        public decimal Vrednost_rabata { get; set; }
        public int MinKolicina { get; set; }
    }
}
