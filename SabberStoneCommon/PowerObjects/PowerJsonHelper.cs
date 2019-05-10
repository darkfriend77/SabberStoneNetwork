using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SabberStoneCore.Enums;
using SabberStoneCore.Kettle;

namespace SabberStoneCommon.PowerObjects
{

    public class KnownTypesBinder : ISerializationBinder
    {
        public IList<System.Type> KnownTypes { get; set; }

        public System.Type BindToType(string assemblyName, string typeName)
        {
            return KnownTypes.SingleOrDefault(t => t.Name == typeName);
        }

        public void BindToName(System.Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }
    }



    public struct PowerHistoryStruct
    {
        public PowerType PowerType { get; set; }
        public string PowerHistoryString { get; set; }
    }

    public class PowerJsonHelper
    {

        public static KnownTypesBinder KnownTypesBinder => new KnownTypesBinder
        {
            KnownTypes = new List<System.Type>
            {
                typeof(PowerHistoryFullEntity),
                typeof(PowerHistoryShowEntity),
                typeof(PowerHistoryHideEntity),
                typeof(PowerHistoryTagChange),
                typeof(PowerHistoryBlockStart),
                typeof(PowerHistoryBlockEnd),
                typeof(PowerHistoryCreateGame),
                typeof(PowerHistoryMetaData)
            }
        };

        public static string Serialize(List<IPowerHistoryEntry> powerHistoryEntries)
        {
            return JsonConvert.SerializeObject(powerHistoryEntries, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                SerializationBinder = KnownTypesBinder
            });
        }

        //public static List<PowerHistoryStruct> Serialize(List<IPowerHistoryEntry> PowerHistoryEntries)
        //{
        //    var powerHistoryStructs = new List<PowerHistoryStruct>();
        //    foreach (var powerHistoryEntry in PowerHistoryEntries)
        //    {
        //        powerHistoryStructs.Add(
        //            new PowerHistoryStruct
        //            {
        //                PowerType = powerHistoryEntry.PowerType,
        //                PowerHistoryString = Serialize(powerHistoryEntry)
        //            });
        //    }
        //    return powerHistoryStructs;
        //}

        public static string Serialize(PowerAllOptions powerAllOptions)
        {
            return "";
        }

        public static string Serialize(IPowerHistoryEntry historyEntry)
        {
            switch (historyEntry.PowerType)
            {
                case PowerType.FULL_ENTITY:
                    return JsonConvert.SerializeObject(historyEntry as PowerHistoryFullEntity);
                case PowerType.SHOW_ENTITY:
                    return JsonConvert.SerializeObject(historyEntry as PowerHistoryShowEntity);
                case PowerType.HIDE_ENTITY:
                    return JsonConvert.SerializeObject(historyEntry as PowerHistoryHideEntity);
                case PowerType.TAG_CHANGE:
                    return JsonConvert.SerializeObject(historyEntry as PowerHistoryTagChange);
                case PowerType.BLOCK_START:
                    return JsonConvert.SerializeObject(historyEntry as PowerHistoryBlockStart);
                case PowerType.BLOCK_END:
                    return JsonConvert.SerializeObject(historyEntry as PowerHistoryBlockEnd);
                case PowerType.CREATE_GAME:
                    return JsonConvert.SerializeObject(historyEntry as PowerHistoryCreateGame);
                case PowerType.META_DATA:
                    return JsonConvert.SerializeObject(historyEntry as PowerHistoryMetaData);
                case PowerType.CHANGE_ENTITY:
                case PowerType.RESET_GAME:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static List<IPowerHistoryEntry> Deserialize(List<PowerHistoryStruct> powerHistoryStructs)
        {
            var powerHistoryEntries = new List<IPowerHistoryEntry>();
            foreach (var powerHistoryStruct in powerHistoryStructs)
            {
                powerHistoryEntries.Add(Deserialize(powerHistoryStruct.PowerType,
                    powerHistoryStruct.PowerHistoryString));
            }
            return powerHistoryEntries;
        }

        public static IPowerHistoryEntry Deserialize(PowerType powerType, string powerHistory)
        {
            switch (powerType)
            {
                case PowerType.FULL_ENTITY:
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<PowerHistoryFullEntity>(powerHistory);
                case PowerType.SHOW_ENTITY:
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<PowerHistoryShowEntity>(powerHistory);
                case PowerType.HIDE_ENTITY:
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<PowerHistoryHideEntity>(powerHistory);
                case PowerType.TAG_CHANGE:
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<PowerHistoryTagChange>(powerHistory);
                case PowerType.BLOCK_START:
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<PowerHistoryBlockStart>(powerHistory);
                case PowerType.BLOCK_END:
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<PowerHistoryBlockEnd>(powerHistory);
                case PowerType.CREATE_GAME:
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<PowerHistoryCreateGame>(powerHistory);
                case PowerType.META_DATA:
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<PowerHistoryMetaData>(powerHistory);
                case PowerType.CHANGE_ENTITY:
                case PowerType.RESET_GAME:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
