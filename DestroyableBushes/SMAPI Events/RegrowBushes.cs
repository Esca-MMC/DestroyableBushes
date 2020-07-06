using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TerrainFeatures;
using Harmony;

namespace DestroyableBushes
{
    public partial class ModEntry : Mod
    {
        private void RegrowBushes(object sender, DayStartedEventArgs e)
        {
            if (Context.IsMainPlayer) //if this is the main player
            {
                if (Data?.DestroyedBushes != null) //if the list of destroyed bushes exists
                {
                    for(int x = Data.DestroyedBushes.Count - 1; x >= 0; x--) //for each destroyed bush (looping backward to allow removal)
                    {
                        var bush = Data.DestroyedBushes[x];

                        if (BushShouldRegrow(bush.DateDestroyed, Config.WhenBushesRegrow)) //if this bush should regrow today
                        {
                            GameLocation location = Game1.getLocationFromName(bush.LocationName);

                            if (location?.isTileOccupiedForPlacement(bush.Tile) == false) //if this bush's tile is NOT obstructed by anything
                            {
                                try
                                {
                                    location.largeTerrainFeatures.Add(new Bush(bush.Tile, bush.Size, location)); //respawn this bush
                                    Data.DestroyedBushes.RemoveAt(x); //remove this bush from the list
                                }
                                catch (Exception ex)
                                {
                                    Instance.Monitor.Log($"Error respawning a bush at {bush.LocationName} ({bush.Tile.X},{bush.Tile.Y}): \n{ex.ToString()}", LogLevel.Error);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Determines whether a bush should regrow today.</summary>
        /// <param name="dateDestroyed">The <see cref="SDate"/> on which the bush was destroyed.</param>
        /// <param name="whenBushesRegrow"><see cref="ModConfig.WhenBushesRegrow"/> or an equivalent string. Describes the amount of time before bushes regrow.</param>
        /// <returns>True if the bush should regrow; otherwise false.</returns>
        private bool BushShouldRegrow(SDate dateDestroyed, string whenBushesRegrow)
        {
            if (dateDestroyed != null && whenBushesRegrow != null) //if the parameters aren't null
            {
                SDate regrowDate = null; //the date when this bush should regrow

                string[] split = whenBushesRegrow.Trim().Split(' '); //split this string into multiple strings around space characters
                if (split.Length >= 2) //if this produced 2 or more strings
                {
                    if (int.TryParse(split[0], out int num)) //if the first string can be parsed into a number
                    {
                        switch (split[split.Length - 1]) //based on the last string (to avoid multiple spaces and similar issues)
                        {
                            case "day":
                            case "days":
                                regrowDate = dateDestroyed.AddDays(num); //regrow "num" days after the provided date
                                break;
                            case "month":
                            case "months":
                            case "season":
                            case "seasons":
                                regrowDate = dateDestroyed.AddDays(28 * num); //get a date "num" seasons after the provided date
                                regrowDate = new SDate(1, regrowDate.Season, regrowDate.Year); //regrow on day 1 of that season/year
                                break;
                            case "year":
                            case "years":
                                regrowDate = new SDate(1, dateDestroyed.Season, dateDestroyed.Year + num); //regrow on day 1 of the same season in "num" years
                                break;
                            default:
                                break;
                        }

                        if (regrowDate != null) //if a valid regrow date was determined
                        {
                            if (SDate.Now() >= regrowDate) //if the regrow date is today OR has already passed
                            {
                                return true; //this bush should regrow
                            }
                        }
                    }
                }
            }

            return false; //this bush should NOT regrow
        }
    }
}
