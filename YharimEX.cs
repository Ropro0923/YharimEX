global using Terraria;
global using Terraria.ModLoader;
global using System;
global using Microsoft.Xna.Framework;
global using YharimEX.Core;
global using YharimEX.Core.Systems;
global using Luminance.Common.Utilities;
global using LumUtils = Luminance.Common.Utilities.Utilities;
using System.IO;
using CalamityMod.Systems;
using Terraria.Graphics.Effects;
using YharimEX.Content.Sky;

namespace YharimEX
{
	public class YharimEX : Mod
	{
        public enum YharimEXMusicMessageType : byte
        {
            MusicEventSyncRequest,
            MusicEventSyncResponse
        }

        public static YharimEX Instance;

        public YharimEX() => Instance = this;

        public override void Load()
        {
            SkyManager.Instance["YharimEX:YharimEXBoss"] = new YharimEXSky();
        }
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            try
            {
                YharimEXMusicMessageType msgType = (YharimEXMusicMessageType)reader.ReadByte();
                switch (msgType)
                {
                    case YharimEXMusicMessageType.MusicEventSyncRequest:
                        {
                            MusicEventSystem.FulfillSyncRequest(whoAmI);
                            break;
                        }

                    case YharimEXMusicMessageType.MusicEventSyncResponse:
                        {
                            MusicEventSystem.ReceiveSyncResponse(reader);
                            break;
                        }

                    default:
                        {
                            YharimEX.Instance.Logger.Error($"Failed to parse VCMM packet: No VCMM packet exists with ID {msgType}.");
                            throw new Exception("Failed to parse VCMM packet: Invalid VCMM packet ID.");
                        }
                }
            }
            catch (Exception e)
            {
                if (e is EndOfStreamException eose)
                {
                    YharimEX.Instance.Logger.Error("Failed to parse VCMM packet: Packet was too short, missing data, or otherwise corrupt.", eose);
                }
                else if (e is ObjectDisposedException ode)
                {
                    YharimEX.Instance.Logger.Error("Failed to parse VCMM packet: Packet reader disposed or destroyed.", ode);
                }
                else if (e is IOException ioe)
                {
                    YharimEX.Instance.Logger.Error("Failed to parse VCMM packet: An unknown I/O error occurred.", ioe);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
