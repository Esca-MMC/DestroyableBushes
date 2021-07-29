using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;

namespace DestroyableBushes
{
    public partial class ModEntry : Mod
    {
        private void SaveModData(object sender, SavingEventArgs e)
        {
            if (Context.IsMainPlayer) //if this is the main player, not a multiplayer farmhand
            {
                try
                {
                    if (Data != null) //if any save data exists
                    {
                        Helper.Data.WriteSaveData<ModData>(ModManifest.UniqueID, Data); //write this mod's data to the player's save file
                    }
                }
                catch (Exception ex)
                {
                    Instance.Monitor.Log($"Error saving this mod's save data:\n{ex.ToString()}", LogLevel.Error);
                    Instance.Monitor.Log($"Previously destroyed bushes might not regrow while this error persists.", LogLevel.Error);
                }
            }
        }
    }
}
