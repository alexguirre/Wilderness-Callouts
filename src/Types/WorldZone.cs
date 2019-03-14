namespace WildernessCallouts.Types
{
    using Rage;
    using Rage.Native;
    using System;

    /// <summary>
    /// Class for zones of the world functions
    /// </summary>
    internal static class WorldZone
    {
        /// <summary>
        /// Returns the position zone, Los Santos or Blaine County
        /// </summary>
        /// <param name="position">Position</param>
        /// <returns>the position zone </returns>
        public static EWorldArea GetArea(Vector3 position)
        {

            EWorldArea zone = (EWorldArea)NativeFunction.Natives.x7ee64d51e8498728<int>(position.X, position.Y, position.Z); //GetMapZoneAtCoords
            return zone;
        }


        /// <summary>
        /// Returns the name of the zone
        /// </summary>
        /// <param name="position">Zone</param>
        /// <returns>the name of the zone</returns>
        public static EWorldZone GetZone(Vector3 Position)
        {
            string zoneId = (string)NativeFunction.Natives.GET_NAME_OF_ZONE<string>(Position.X, Position.Y, Position.Z);

            EWorldZone result;
            if (Enum.TryParse<EWorldZone>(zoneId, true, out result))
            {
                return result;
            }
            else
            {
                return EWorldZone.NULL;
            }
        }


        ///// <summary>
        ///// Returns a ExtendedPosition
        ///// </summary>
        ///// <param name="position">Position</param>
        ///// <returns>a ExtendedPosition</returns>
        //public static ExtendedPosition GetExtendedPosition(Vector3 position)
        //{
        //    ExtendedPosition z = new ExtendedPosition();
        //    z.Position = position;
        //    return z;
        //}


        /// <summary>
        /// Gets the area name
        /// </summary>
        /// <param name="area">Area</param>
        /// <returns>the area name</returns>
        public static string GetAreaName(EWorldArea area)
        {
            String name = Enum.GetName(typeof(EWorldArea), area);
            return name.Replace("_", " ");
        }



        /// <summary>
        /// Gets the zone name
        /// </summary>
        /// <param name="zone">Zone</param>
        /// <returns>the zone name</returns>
        public static string GetZoneName(EWorldZone zone)
        {
            switch (zone)
            {
                case EWorldZone.AIRP:
                    return "Los Santos International Airport";
                case EWorldZone.ALAMO:
                    return "The Alamo Sea";
                case EWorldZone.ALTA:
                    return "Alta";
                case EWorldZone.ARMYB:
                    return "Fort Zancudo";
                case EWorldZone.BANHAMC:
                    return "Banham Canyon";
                case EWorldZone.BANNING:
                    return "Banning";
                case EWorldZone.BEACH:
                    return "Vespucci Beach";
                case EWorldZone.BHAMCA:
                    return "Banham Canyon Drive";
                case EWorldZone.BRADP:
                    return "Braddock Pass";
                case EWorldZone.BRADT:
                    return "Braddock Tunnel";
                case EWorldZone.BURTON:
                    return "Burton";
                case EWorldZone.CALAFB:
                    return "Calafia Bridge";
                case EWorldZone.CANNY:
                    return "Raton Canyon";
                case EWorldZone.CCREAK:
                    return "Cassidy Creek";
                case EWorldZone.CHAMH:
                    return "Chamberlain Hills";
                case EWorldZone.CHIL:
                    return "Vinewood Hills";
                case EWorldZone.CHU:
                    return "Chumash";
                case EWorldZone.CMSW:
                    return "Chiliad Mountain State Wilderness";
                case EWorldZone.CYPRE:
                    return "Cypress Flats";
                case EWorldZone.DAVIS:
                    return "Davis";
                case EWorldZone.DELBE:
                    return "Del Perro Beach";
                case EWorldZone.DELPE:
                    return "Del Perro";
                case EWorldZone.DELSOL:
                    return "Puerto Del Sol";
                case EWorldZone.DESRT:
                    return "Grand Senora Desert";
                case EWorldZone.DOWNT:
                    return "Downtown";
                case EWorldZone.DTVINE:
                    return "Downtown Vinewood";
                case EWorldZone.EAST_V:
                    return "East Vinewood";
                case EWorldZone.EBURO:
                    return "El Burro Heights";
                case EWorldZone.ELGORL:
                    return "El Gordo Lighthouse";
                case EWorldZone.ELYSIAN:
                    return "Elysian Island";
                case EWorldZone.GALFISH:
                    return "Galilee";
                case EWorldZone.golf:
                    return "GWC and Golfing Society";
                case EWorldZone.GRAPES:
                    return "Grapeseed";
                case EWorldZone.GREATC:
                    return "Great Chaparral";
                case EWorldZone.HARMO:
                    return "Harmony";
                case EWorldZone.HAWICK:
                    return "Hawick";
                case EWorldZone.HORS:
                    return "Vinewood Racetrack";
                case EWorldZone.HUMLAB:
                    return "Humane Labs and Research";
                case EWorldZone.JAIL:
                    return "Bolingbroke Penitentiary";
                case EWorldZone.KOREAT:
                    return "Little Seoul";
                case EWorldZone.LACT:
                    return "Land Act Reservoir";
                case EWorldZone.LAGO:
                    return "Lago Zancudo";
                case EWorldZone.LDAM:
                    return "Land Act Dam";
                case EWorldZone.LEGSQU:
                    return "Legion Square";
                case EWorldZone.LMESA:
                    return "La Mesa";
                case EWorldZone.LOSPUER:
                    return "La Puerta";
                case EWorldZone.MIRR:
                    return "Mirror Park";
                case EWorldZone.MORN:
                    return "Morningwood";
                case EWorldZone.MOVIE:
                    return "Richards Majestic";
                case EWorldZone.MTCHIL:
                    return "Mount Chiliad";
                case EWorldZone.MTGORDO:
                    return "Mount Gordo";
                case EWorldZone.MTJOSE:
                    return "Mount Josiah";
                case EWorldZone.MURRI:
                    return "Murrieta Heights";
                case EWorldZone.NCHU:
                    return "North Chumash";
                case EWorldZone.NOOSE:
                    return "NOOSE HQ";
                case EWorldZone.OCEANA:
                    return "Pacific Ocean";
                case EWorldZone.PALCOV:
                    return "Paleto Cove";
                case EWorldZone.PALETO:
                    return "Paleto Bay";
                case EWorldZone.PALFOR:
                    return "Paleto Forest";
                case EWorldZone.PALHIGH:
                    return "Palomino Highlands";
                case EWorldZone.PALMPOW:
                    return "Palmer-Taylor Power Station";
                case EWorldZone.PBLUFF:
                    return "Pacific Bluffs";
                case EWorldZone.PBOX:
                    return "Pillbox Hill";
                case EWorldZone.PROCOB:
                    return "Procopio Beach";
                case EWorldZone.RANCHO:
                    return "Rancho";
                case EWorldZone.RGLEN:
                    return "Richman Glen";
                case EWorldZone.RICHM:
                    return "Richman";
                case EWorldZone.ROCKF:
                    return "Rockford Hills";
                case EWorldZone.RTRAK:
                    return "Redwood Lights Track";
                case EWorldZone.SanAnd:
                    return "San Andreas";
                case EWorldZone.SANCHIA:
                    return "San Chianski Mountain Range";
                case EWorldZone.SANDY:
                    return "Sandy Shores";
                case EWorldZone.SKID:
                    return "Mission Row";
                case EWorldZone.SLAB:
                    return "Stab City";
                case EWorldZone.STAD:
                    return "Maze Bank Arena";
                case EWorldZone.STRAW:
                    return "Strawberry";
                case EWorldZone.TATAMO:
                    return "Tataviam Mountains";
                case EWorldZone.TERMINA:
                    return "Terminal";
                case EWorldZone.TEXTI:
                    return "Textile City";
                case EWorldZone.TONGVAH:
                    return "Tongva Hills";
                case EWorldZone.TONGVAV:
                    return "Tongva Valley";
                case EWorldZone.VCANA:
                    return "Vespucci Canals";
                case EWorldZone.VESP:
                    return "Vespucci";
                case EWorldZone.VINE:
                    return "Vinewood";
                case EWorldZone.WINDF:
                    return "RON Alternates Wind Farm";
                case EWorldZone.WVINE:
                    return "West Vinewood";
                case EWorldZone.ZANCUDO:
                    return "Zancudo River";
                case EWorldZone.ZP_ORT:
                    return "Port of South Los Santos";
                case EWorldZone.ZQ_UAR:
                    return "Davis Quartz";
                default:
                    return String.Empty;
            }
        }


        /// <summary>
        /// Gets the street name
        /// </summary>
        /// <param name="position">Position</param>
        /// <returns>the street name</returns>
        public static string GetStreetName(Vector3 position)
        {
            return World.GetStreetName(position);
        }
    }
}
