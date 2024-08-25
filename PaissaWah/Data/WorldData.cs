using System.Collections.Generic;

namespace PaissaWah.Data
{
    public static class WorldData
    {
        public static Dictionary<string, List<string>> GetWorldsByDatacenter()
        {
            return new Dictionary<string, List<string>>()
            {
                // NA
                { "Aether", new List<string> { "Adamantoise", "Cactuar", "Faerie", "Gilgamesh", "Jenova", "Midgardsormr", "Sargatanas", "Siren" }},
                { "Primal", new List<string> { "Behemoth", "Excalibur", "Exodus", "Famfrit", "Hyperion", "Lamia", "Leviathan", "Ultros" }},
                { "Crystal", new List<string> { "Balmung", "Brynhildr", "Coeurl", "Diabolos", "Goblin", "Malboro", "Mateus", "Zalera" }},
                { "Dynamis", new List<string> { "Cuchulainn", "Golem", "Halicarnassus", "Kraken", "Maduin", "Marilith", "Rafflesia", "Seraph" }},

                // EU
                { "Chaos", new List<string> { "Cerberus", "Louisoix", "Moogle", "Omega", "Ragnarok", "Spriggan" }},
                { "Light", new List<string> { "Lich", "Odin", "Phoenix", "Shiva", "Twintania", "Zodiark" }},
                { "Shadow", new List<string> { "Innocence", "Pixie", "Titania", "Tycoon" }},

                //JP
                { "Elemental", new List<string> { "Aegis", "Atomos", "Carbuncle", "Garuda", "Gungnir", "Kujata", "Ramuh", "Tonberry", "Typhon", "Unicorn" }},
                { "Gaia", new List<string> { "Alexander", "Bahamut", "Durandal", "Fenrir", "Ifrit", "Ridill", "Tiamat", "Ultima", "Valefor", "Yojimbo", "Zeromus" }},
                { "Mana", new List<string> { "Anima", "Asura", "Chocobo", "Hades", "Ixion", "Mandragora", "Masamune", "Pandaemonium", "Shinryu", "Titan" }},
                { "Meteor", new List<string> { "Belias", "Mandragora", "Ramuh", "Shinryu", "Unicorn", "Valefor", "Yojimbo", "Zeromus" }},

                //OCE
                { "Materia", new List<string> { "Bismarck", "Ravana", "Sephirot", "Sophia", "Zurvan" }},
            };
        }
    }
}
