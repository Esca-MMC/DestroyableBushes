using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace DestroyableBushes
{
    public static partial class Commands
    {
        /// <summary>SMAPI console command. Creates a new bush instance with specific properties.</summary>
        /// <param name="command">The console command's name.</param>
        /// <param name="args">The space-delimited arguments entered with the command.</param>
        /// <remarks>Expected format: add_bush &lt;int size&gt; [bool townBush] [int tileSheetOffset] [int x int y] [string location]</remarks>
        public static void AddBush(string command, string[] args)
        {
            if (!Context.IsWorldReady) //if the player is not currently in a loaded game
                return; //do nothing                

            /* parse args */

            if (args.Length <= 0) //"add_bush" (invalid)
            {
                ModEntry.Instance.Monitor.Log($"Invalid number of arguments. Please include a bush size, e.g. \"{command} {Bush.largeBush}\". Type \"help {command}\" for more info.", LogLevel.Info);
                return;
            }
            else if (args.Length == 4) //"add_bush <size> [townBush] [tileSheetOffset] [x]" (invalid)
            {
                ModEntry.Instance.Monitor.Log($"Invalid number of arguments. Please include both X and Y, e.g. \"{command} {Bush.largeBush} false 0 64 19\". Type \"help {command}\" for more info.", LogLevel.Info);
                return;
            }

            if (!TryParseBushSize(args[0], out int size)) //"add_bush <size>"
            {
                ModEntry.Instance.Monitor.Log($"\"{args[0]}\" is not a recognized size value. Type \"help {command}\" for formatting information.", LogLevel.Info);
                return;
            }

            bool townBush = false;
            if (args.Length > 1 && !ArgUtility.TryGetBool(args, 1, out townBush, out _, "bool townBush")) //"add_bush <size> [townBush]"
            {
                ModEntry.Instance.Monitor.Log($"\"{args[1]}\" is not a recognized townBush value. It should be \"true\" or \"false\". Type \"help {command}\" for more info.", LogLevel.Info);
                return;
            }

            int tileSheetOffset = 0;
            if (args.Length > 2 && !ArgUtility.TryGetInt(args, 2, out tileSheetOffset, out _, "int tileSheetOffset")) //"add_bush <size> [townBush] [tileSheetOffset]"
            {
                ModEntry.Instance.Monitor.Log($"\"{args[2]}\" is not a recognized tileSheetOffset value. It should be an integer, e.g. \"0\". Type \"help {command}\" for more info.", LogLevel.Info);
                return;
            }

            Vector2 tile;
            bool usingPlayerTile;

            if (args.Length <= 4) //if x and y weren't provided as args
            {
                tile = Game1.player.Tile;
                usingPlayerTile = true;
            }
            else if (!ArgUtility.TryGetVector2(args, 3, out tile, out _, true, "Vector2 tile")) //"add_bush <size> [townBush] [tileSheetOffset] [x y]"
            {
                ModEntry.Instance.Monitor.Log($"\"{args[3]} {args[4]}\" are not recognized x and y values. They should be integers, e.g. \"64 19\". Type \"help {command}\" for more info.", LogLevel.Info);
                return;
            }
            else //if x and y were provided as args, and parsing succeeded
            {
                usingPlayerTile = false;
            }

            GameLocation location;

            if (args.Length > 5 && args[5] != null) //"add_bush <size> [townBush] [tileSheetOffset] [x y] [location]"
            {
                location = Game1.getLocationFromName(args[5]); //attempt to get a location with the combined name
                if (location == null) //if no location matched the name
                {
                    ModEntry.Instance.Monitor.Log($"No location named \"{args[5]}\" could be found. Type \"help {command}\" for more info.", LogLevel.Info);
                    return;
                }
            }
            else //location wasn't provided as an argument
            {
                location = Game1.player.currentLocation; //use the player's current location
                if (location == null) //if the player doesn't have a location for some reason
                    return;
            }

            /* args parsed; spawn bush */

            Bush bush = new Bush(tile, size, location);

            bush.townBush.Value = townBush;
            bush.tileSheetOffset.Value = tileSheetOffset;
            bush.setUpSourceRect(); //update the bush's sprite (necessary if the fields above change)

            if (usingPlayerTile) //if the player's tile was used to spawn this bush
            {
                Rectangle playerBox = Game1.player.GetBoundingBox();
                while (playerBox.Intersects(bush.getBoundingBox())) //while this bush is colliding with the player
                {
                    switch (Game1.player.FacingDirection) //based on which direction the player is facing
                    {
                        default: //unknown facing position (should be unreachable)
                        case 0: //up
                            bush.Tile = new Vector2(bush.Tile.X, bush.Tile.Y - 1); //move bush up 1 tile
                            break;
                        case 1: //right
                            bush.Tile = new Vector2(bush.Tile.X + 1, bush.Tile.Y); //move bush right 1 tile
                            break;
                        case 2: //down
                            bush.Tile = new Vector2(bush.Tile.X, bush.Tile.Y + 1); //move bush down 1 tile
                            break;
                        case 3: //left
                            bush.Tile = new Vector2(bush.Tile.X - 1, bush.Tile.Y); //move bush left 1 tile
                            break;
                    }
                }
            }

            location.largeTerrainFeatures.Add(bush); //add the bush to the location
        }

        /// <summary>Attempts to parse a text string describing a bush's size (a.k.a. type) into an integer value.</summary>
        /// <param name="rawSize">A string describing a bush size.</param>
        /// <param name="parsedSize">The parsed integer value for this bush size. 0 if parsing failed.</param>
        /// <returns>True if parsing succeeded. False if parsing failed, i.e. if the raw size was not an integer or recognized name.</returns>
        private static bool TryParseBushSize(string rawSize, out int parsedSize)
        {
            string normalizedRawSize = rawSize?.Trim().ToLower();
            switch (normalizedRawSize)
            {
                case "0":
                case "s":
                case "small":
                    parsedSize = Bush.smallBush;
                    return true;
                case "1":
                case "m":
                case "med":
                case "medium":
                    parsedSize = Bush.mediumBush;
                    return true;
                case "2":
                case "l":
                case "large":
                    parsedSize = Bush.largeBush;
                    return true;
                case "3":
                case "t":
                case "green":
                case "greentea":
                case "greenteabush":
                case "tea":
                case "teabush":
                    parsedSize = Bush.greenTeaBush;
                    return true;
                case "4":
                case "w":
                case "walnut":
                    parsedSize = Bush.walnutBush;
                    return true;
                default:
                    return int.TryParse(normalizedRawSize, out parsedSize); //true if the input can be parsed into any other integer

            }
        }
    }
}
