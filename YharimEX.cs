global using Luminance.Common.Utilities;
global using LumUtils = Luminance.Common.Utilities.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalamityMod.Systems;
using Terraria.ModLoader;

namespace YharimEX
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class YharimEX : Mod
	{
        public enum YharimEXMusicMessageType : byte
        {
            MusicEventSyncRequest,
            MusicEventSyncResponse
        }

        public static YharimEX Instance;

        public YharimEX() => Instance = this;

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
