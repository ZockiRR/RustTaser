using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ElectricTaser", "ZockiRR", "1.0.2")]
    [Description("Gives players the ability to spawn a taser")]
    class ElectricTaser : RustPlugin
    {

        #region variables
        private const string PERMISSION_GIVETASER = "electrictaser.givetaser";
        #endregion variables

        #region Configuration

        private Configuration config;

        private class Configuration
        {
            [JsonProperty("TaserCooldown")]
            public float TaserCooldown = 5f;

            [JsonProperty("TaserDistance")]
            public float TaserDistance = 8f;

            [JsonProperty("TaserShockDuration")]
            public float TaserShockDuration = 20f;

            [JsonProperty("TaserDamage")]
            public float TaserDamage = 0f;

            [JsonProperty("ItemNailgun")]
            public string ItemNailgun = "pistol.nailgun";

            [JsonProperty("PrefabScream")]
            public string PrefabScream = "assets/bundled/prefabs/fx/player/gutshot_scream.prefab";

            [JsonProperty("PrefabShock")]
            public string PrefabShock = "assets/prefabs/locks/keypad/effects/lock.code.shock.prefab";

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                PrintWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            PrintWarning($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion Configuration

        #region localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermissionGiveTaser"] = "You are not allowed to spawn a taser",
                ["PlayerNameNotExistent"] = "The name matches no player",
                ["GaveTaserTo"] = "Gave taser to {0}",
                ["GaveTaserToYou"] = "Gave taser to you",
                ["CouldNotGive"] = "Could not spawn a taser",
                ["Taser"] = "Taser"
            }, this);
        }
        #endregion localization

        #region chatommands
        [ChatCommand("givetaser")]
        private void GiveTaser(BasePlayer aPlayer, string aCommand, string[] someArgs)
        {
            if (!permission.UserHasPermission(aPlayer.UserIDString, PERMISSION_GIVETASER))
            {
                aPlayer.ChatMessage(Lang("NoPermissionGiveTaser", aPlayer.UserIDString));
                return;
            }

            BasePlayer thePlayer = someArgs.Length > 0 ? FindPlayerByName(aPlayer, someArgs[0]) : aPlayer;
            if(!thePlayer)
            {
                return;
            }
            Item theItem = GiveItemToPlayer(thePlayer, config.ItemNailgun);
            if (theItem == null)
            {
                aPlayer.ChatMessage(Lang("CouldNotGive", aPlayer.UserIDString));
                return;
                
            }
            BaseProjectile theTaser = theItem.GetHeldEntity().GetComponent<BaseProjectile>();
            theItem.name = Lang("Taser");
            theTaser.canUnloadAmmo = false;
            theTaser.primaryMagazine.contents = 1;
            theTaser.primaryMagazine.capacity = 0;
            theTaser.gameObject.AddComponent<TaserController>().Config = config;
            theTaser.SendNetworkUpdateImmediate();
            if (thePlayer == aPlayer)
            {
                aPlayer.ChatMessage(Lang("GaveTaserToYou", aPlayer.UserIDString));
            }
            else
            {
                aPlayer.ChatMessage(Lang("GaveTaserTo", aPlayer.UserIDString, thePlayer.displayName));
            }
        }
        #endregion chatommands

        #region hooks
        private void Init()
        {
            permission.RegisterPermission(PERMISSION_GIVETASER, this);
        }

        private void Unload()
        {
            foreach (BaseProjectile eachProjectile in BaseNetworkable.serverEntities.OfType<BaseProjectile>())
            {
                TaserController theController = eachProjectile.GetComponent<TaserController>();
                if (theController)
                {
                    eachProjectile.GetItem()?.Remove();
                    if (!eachProjectile.IsDestroyed)
                    {
                        eachProjectile.Kill();
                    }
                }
            }

            foreach (BasePlayer eachProjectile in BaseNetworkable.serverEntities.OfType<BasePlayer>())
            {
                UnityEngine.Object.Destroy(eachProjectile.GetComponent<ShockedController>());
            }
        }

        private void OnWeaponFired(BaseProjectile aProjectile, BasePlayer aPlayer, ItemModProjectile aMod, ProtoBuf.ProjectileShoot aProjectileProtoBuf)
        {
            aProjectile.GetComponent<TaserController>()?.ResetTaser();
        }

        private object CanCreateWorldProjectile(HitInfo anInfo, ItemDefinition anItemDefinition)
        {
            if (anInfo.Weapon.GetComponent<TaserController>()) {
                return false;
            }
            return null;
        }

        private object OnEntityTakeDamage(BaseCombatEntity anEntity, HitInfo aHitInfo)
        {
            if (!aHitInfo?.Weapon?.GetComponent<TaserController>())
            {
                return null;
            }

            aHitInfo.damageTypes.Clear();
            aHitInfo.DoHitEffects = false;
            aHitInfo.DoDecals = false;
            float theDistance = !aHitInfo.IsProjectile() ? Vector3.Distance(aHitInfo.PointStart, aHitInfo.HitPositionWorld) : aHitInfo.ProjectileDistance;
            if (config.TaserDistance > 0f && theDistance > config.TaserDistance)
            {
                aHitInfo.DidHit = false;
                return false;
            }
            Effect.server.Run(config.PrefabShock, anEntity, aHitInfo.HitBone, aHitInfo.HitPositionLocal, aHitInfo.HitNormalLocal);
            aHitInfo.damageTypes.Add(Rust.DamageType.ElectricShock, config.TaserDamage);
            BasePlayer thePlayer = anEntity?.GetComponent<BasePlayer>();
            if (thePlayer)
            {
                ShockedController theController = thePlayer.GetComponent<ShockedController>();
                if (!theController)
                {
                    theController = thePlayer.gameObject.AddComponent<ShockedController>();
                    theController.Config = config;
                }
                NextFrame(() => theController.Shock(aHitInfo));
            }
            return null;
        }

        #endregion hooks

        #region methods
        private Item GiveItemToPlayer(BasePlayer aPlayer, string anItemName)
        {
            Item theItem = ItemManager.Create(ItemManager.FindItemDefinition(anItemName.ToLower()));
            if (theItem == null)
            {
                return null;
            }
            if (!aPlayer.inventory.GiveItem(theItem))
            {
                theItem.Remove();
                return null;
            }
            return theItem;
        }
        #endregion methods

        #region helpers

        private BasePlayer FindPlayerByName(BasePlayer aPlayer, string aName)
        {
            BasePlayer thePlayer = Player.Find(aName);
            if (!thePlayer)
            {
                aPlayer.ChatMessage(Lang("PlayerNameNotExistent", aPlayer.UserIDString));
                return null;
            }
            return thePlayer;
        }

        private string Lang(string aKey, string aUserID = null, params object[] someArgs) => string.Format(lang.GetMessage(aKey, this, aUserID), someArgs);
        #endregion helpers

        #region controllers
        private class TaserController : FacepunchBehaviour
        {
            public Configuration Config { get; set; }
            private BaseProjectile Taser
            {
                get
                {
                    if (taser == null)
                    {
                        taser = GetComponent<BaseProjectile>();
                    }
                    return taser;
                }
            }

            private BaseProjectile taser;

            public void ResetTaser()
            {
                Invoke(() =>
                {
                    Taser.primaryMagazine.contents = 1;
                    Taser.SendNetworkUpdateImmediate();
                }, Config.TaserCooldown);
            }
        }

        private class ShockedController : FacepunchBehaviour
        {
            public Configuration Config { get; set; }
            private BasePlayer Player
            {
                get
                {
                    if (player == null)
                    {
                        player = GetComponent<BasePlayer>();
                    }
                    return player;
                }
            }

            private BasePlayer player;

            public void Shock(HitInfo aHitInfo)
            {
                Effect.server.Run(Config.PrefabScream, Player.transform.position);
                if (!Player.IsSleeping())
                {
                    Player.StartWounded(aHitInfo?.InitiatorPlayer, aHitInfo);
                    Player.woundedStartTime = Time.realtimeSinceStartup;
                    Player.woundedDuration = Config.TaserShockDuration + 5f;
                    CancelInvoke(StopWounded);
                    Invoke(StopWounded, Config.TaserShockDuration);
                }
            }

            private void StopWounded()
            {
                if (Player.IsWounded())
                {
                    Player.StopWounded();
                }
            }
        }
        #endregion controllers
    }
}
