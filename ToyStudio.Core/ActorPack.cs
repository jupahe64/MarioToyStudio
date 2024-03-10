using BymlLibrary;
using SarcLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ToyStudio.Core.common;
using ToyStudio.Core.util;
using ToyStudio.Core.util.byml_serialization;
using static ToyStudio.Core.ActorPack;

namespace ToyStudio.Core
{
    public interface IActorPackAttachment
    {
        void Unload();
    }

    public partial class ActorPack
    {
        public string Name => _name;
        public string? Category => GetActorInfoValue(x=>x.Category);

        public ActorPack(string name, Dictionary<string, byte[]> entries)
        {
            _name = name;
            _entries = entries;

            var actorInfo = LoadBymlObject<ActorInfo>(BgymlTypeInfos.ActorParam.GetPath(name));
            _actorInfoHierarchy.Add(actorInfo);

            while(actorInfo.ParentName != null)
            {
                name = actorInfo.ParentName;
                actorInfo = LoadBymlObject<ActorInfo>(BgymlTypeInfos.ActorParam.GetPath(name));
                _actorInfoHierarchy.Add(actorInfo);
            }
        }

        internal T? GetActorInfoValue<T>(Func<ActorInfo, T?> getter)
        {
            foreach (var actorInfo in _actorInfoHierarchy)
            {
                T? value = getter(actorInfo);
                if (value is not null)
                    return value;
            }

            return default;
        }

        internal void SetActorInfoValue<T>(Action<ActorInfo> setter)
        {
            setter.Invoke(_actorInfoHierarchy[0]);
        }

        internal bool TryLoadBymlObject<T>(string[] filePath, [NotNullWhen(true)] out T? bymlObject)
            where T : IBymlObject<T>
        {
            if (!_entries.TryGetValue(string.Join('/', filePath), out byte[]? bytes))
            {
                bymlObject = default;
                return false;
            }

            var byml = Byml.FromBinary(_entries[string.Join('/', filePath)]);
            bymlObject = T.Deserialize(byml);
            return true;
        }

        internal T LoadBymlObject<T>(string[] filePath)
            where T : IBymlObject<T>
        {
            if (!_entries.TryGetValue(string.Join('/', filePath), out byte[]? bytes))
                throw new FileNotFoundException("Couldn't find " +
                    $"{string.Join('/', filePath)} in Pack: {_name}");

            var byml = Byml.FromBinary(bytes);
            return T.Deserialize(byml);
        }

        internal void SaveBymlObject<T>(string[] filePath, T bymlObject)
            where T : IBymlObject<T>
        {
            var byml = bymlObject.Serialize();
            _entries[string.Join('/', filePath)] = byml.ToBinary(Revrs.Endianness.Little);
        }

        public void Attach<T>(T attachment)
            where T : class, IActorPackAttachment
        {
            var type = typeof(T);
            if (_attachments.ContainsKey(type))
                throw new InvalidOperationException(
                    $"ActorPack {_name} already has attachment of type {type.Name}");

            _attachments[type] = attachment;
        }

        public bool TryGetAttachment<T>([NotNullWhen(true)] out T? attachment)
            where T : class, IActorPackAttachment
        {
            bool success = _attachments.TryGetValue(typeof(T), out var value);
            if (success)
                attachment = (value as T);
            else
                attachment = null;
            return success;
        }

        internal void Unload()
        {
            foreach (var type in _attachments.Keys.ToList())
            {
                _attachments.Remove(type, out var attachment);
                attachment!.Unload();
            }
        }

        private readonly string _name;
        private readonly Dictionary<string, byte[]> _entries;
        private readonly Dictionary<Type, IActorPackAttachment> _attachments = [];
        private readonly List<ActorInfo> _actorInfoHierarchy = [];

        

        public partial class ActorInfo : BymlObject<ActorInfo>
        {
            public string? ParentName = null;
            public string? Category = null;

            public ComponentsObject? Components => _components;
            
            protected override void Deserialize(Deserializer d)
            {
                d.SetBgymlRefName(ref ParentName!, "$parent", BgymlTypeInfos.ActorParam);
                d.SetString(ref Category!, "Category");
                d.SetObject(ref _components!, "Components");
            }

            protected override void Serialize(Serializer s)
            {
                s.SetBgymlRefName(ref ParentName!, "$parent", BgymlTypeInfos.ActorParam);
                s.SetString(ref Category!, "Category");
                s.SetObject(ref _components!, "Components");
            }

            public class ComponentsObject : BymlObject<ComponentsObject>
            {
                public string? AIInfoRefName;
                public string? ASInfoRefName;
                public string? ASOptimizeName;
                //TODO ASRef
                public string? AnimationRefName;
                public string? BlackboardRefName;
                public string? Collision2DRefName;
                public string? DropShadowRefName;
                public string? ELinkRefName;
                public string? GameParameterTableRefName;
                public string? LookAtRefName;
                public string? ModelBindRefName;
                public string? ModelInfoRefName;
                public string? Movement2DRefName;
                public string? ObjStateInfoRefName;
                public string? PauseExemptRefName;
                public string? PlayerMovement2dParamRefName;
                public string? PlayerStateInfoRefName;
                public string? RespawnRefName;
                public string? SLinkRefName;
                public string? SystemSettingName;
                public string? XLinkRefName;

                protected override void Deserialize(Deserializer d)
                {
                    d.SetBgymlRefName(ref AIInfoRefName!, "AIInfoRef", 
                        BgymlTypeInfos.AIInfo, isWork: false);
                    d.SetBgymlRefName(ref ASInfoRefName!, "ASInfoRef", 
                        BgymlTypeInfos.ASInfo, isWork: false);
                    d.SetBgymlRefName(ref ASOptimizeName!, "ASOptimize", 
                        BgymlTypeInfos.ASOptimize, isWork: false);
                    d.SetBgymlRefName(ref AnimationRefName!, "AnimationRef", 
                        BgymlTypeInfos.AnimationParam, isWork: false);
                    d.SetBgymlRefName(ref BlackboardRefName!, "BlackboardRef", 
                        BgymlTypeInfos.BlackboardInfo, isWork: false);
                    d.SetBgymlRefName(ref Collision2DRefName!, "Collision2DRef", 
                        BgymlTypeInfos.Collision2DParam, isWork: false);
                    d.SetBgymlRefName(ref DropShadowRefName!, "DropShadowRef", 
                        BgymlTypeInfos.DropShadowParam, isWork: false);
                    d.SetBgymlRefName(ref ELinkRefName!, "ELinkRef", 
                        BgymlTypeInfos.ELinkParam, isWork: false);
                    d.SetBgymlRefName(ref GameParameterTableRefName!, "GameParameterTableRef", 
                        BgymlTypeInfos.GameParameterTable, isWork: false);
                    d.SetBgymlRefName(ref LookAtRefName!, "LookAtRef", 
                        BgymlTypeInfos.LookAtParam, isWork: false);
                    d.SetBgymlRefName(ref ModelBindRefName!, "ModelBindRef", 
                        BgymlTypeInfos.ModelBindParam, isWork: false);
                    d.SetBgymlRefName(ref ModelInfoRefName!, "ModelInfoRef", 
                        BgymlTypeInfos.ModelInfo, isWork: false);
                    d.SetBgymlRefName(ref Movement2DRefName!, "Movement2DRef", 
                        BgymlTypeInfos.Movement2DParam, isWork: false);
                    d.SetBgymlRefName(ref ObjStateInfoRefName!, "ObjStateInfoRef", 
                        BgymlTypeInfos.ObjStateInfoParam, isWork: false);
                    d.SetBgymlRefName(ref PauseExemptRefName!, "PauseExemptRef", 
                        BgymlTypeInfos.PauseExemptParam, isWork: false);
                    d.SetBgymlRefName(ref PlayerMovement2dParamRefName!, "PlayerMovement2dParamRef", 
                        BgymlTypeInfos.PlayerMovement2dParam, isWork: false);
                    d.SetBgymlRefName(ref PlayerStateInfoRefName!, "PlayerStateInfoRef", 
                        BgymlTypeInfos.PlayerStateInfoParam, isWork: false);
                    d.SetBgymlRefName(ref RespawnRefName!, "RespawnRef", 
                        BgymlTypeInfos.RespawnParam, isWork: false);
                    d.SetBgymlRefName(ref SLinkRefName!, "SLinkRef", 
                        BgymlTypeInfos.SLinkParam, isWork: false);
                    d.SetBgymlRefName(ref SystemSettingName!, "SystemSetting", 
                        BgymlTypeInfos.ActorSystemSetting, isWork: false);
                    d.SetBgymlRefName(ref XLinkRefName!, "XLinkRef", 
                        BgymlTypeInfos.XLinkParam, isWork: false);
                }

                protected override void Serialize(Serializer s)
                {
                    s.SetBgymlRefName(ref AIInfoRefName!, "AIInfoRef",
                        BgymlTypeInfos.AIInfo, isWork: false);
                    s.SetBgymlRefName(ref ASInfoRefName!, "ASInfoRef",
                        BgymlTypeInfos.ASInfo, isWork: false);
                    s.SetBgymlRefName(ref ASOptimizeName!, "ASOptimize",
                        BgymlTypeInfos.ASOptimize, isWork: false);
                    s.SetBgymlRefName(ref AnimationRefName!, "AnimationRef",
                        BgymlTypeInfos.AnimationParam, isWork: false);
                    s.SetBgymlRefName(ref BlackboardRefName!, "BlackboardRef",
                        BgymlTypeInfos.BlackboardInfo, isWork: false);
                    s.SetBgymlRefName(ref Collision2DRefName!, "Collision2DRef",
                        BgymlTypeInfos.Collision2DParam, isWork: false);
                    s.SetBgymlRefName(ref DropShadowRefName!, "DropShadowRef",
                        BgymlTypeInfos.DropShadowParam, isWork: false);
                    s.SetBgymlRefName(ref ELinkRefName!, "ELinkRef",
                        BgymlTypeInfos.ELinkParam, isWork: false);
                    s.SetBgymlRefName(ref GameParameterTableRefName!, "GameParameterTableRef",
                        BgymlTypeInfos.GameParameterTable, isWork: false);
                    s.SetBgymlRefName(ref LookAtRefName!, "LookAtRef",
                        BgymlTypeInfos.LookAtParam, isWork: false);
                    s.SetBgymlRefName(ref ModelBindRefName!, "ModelBindRef",
                        BgymlTypeInfos.ModelBindParam, isWork: false);
                    s.SetBgymlRefName(ref ModelInfoRefName!, "ModelInfoRef",
                        BgymlTypeInfos.ModelInfo, isWork: false);
                    s.SetBgymlRefName(ref Movement2DRefName!, "Movement2DRef",
                        BgymlTypeInfos.Movement2DParam, isWork: false);
                    s.SetBgymlRefName(ref ObjStateInfoRefName!, "ObjStateInfoRef",
                        BgymlTypeInfos.ObjStateInfoParam, isWork: false);
                    s.SetBgymlRefName(ref PauseExemptRefName!, "PauseExemptRef",
                        BgymlTypeInfos.PauseExemptParam, isWork: false);
                    s.SetBgymlRefName(ref PlayerMovement2dParamRefName!, "PlayerMovement2dParamRef",
                        BgymlTypeInfos.PlayerMovement2dParam, isWork: false);
                    s.SetBgymlRefName(ref PlayerStateInfoRefName!, "PlayerStateInfoRef",
                        BgymlTypeInfos.PlayerStateInfoParam, isWork: false);
                    s.SetBgymlRefName(ref RespawnRefName!, "RespawnRef",
                        BgymlTypeInfos.RespawnParam, isWork: false);
                    s.SetBgymlRefName(ref SLinkRefName!, "SLinkRef",
                        BgymlTypeInfos.SLinkParam, isWork: false);
                    s.SetBgymlRefName(ref SystemSettingName!, "SystemSetting",
                        BgymlTypeInfos.ActorSystemSetting, isWork: false);
                    s.SetBgymlRefName(ref XLinkRefName!, "XLinkRef",
                        BgymlTypeInfos.XLinkParam, isWork: false);
                }
            }

            ComponentsObject? _components;
        }
    }

    public static class ActorPackExtensions
    {
        public static T GetOrCreateAttachment<T>(this ActorPack pack, out bool wasAlreadyPresent)
            where T : class, IActorPackAttachment, new()
        {
            if (pack.TryGetAttachment(out T? attachment))
            {
                wasAlreadyPresent = true;
                return attachment;
            }

            attachment = new T();
            pack.Attach(attachment);
            wasAlreadyPresent = false;
            return attachment;
        }

        public static T GetOrCreateAttachment<T>(this ActorPack pack, Func<T> creator, out bool wasAlreadyPresent)
            where T : class, IActorPackAttachment
        {
            if (pack.TryGetAttachment(out T? attachment))
            {
                wasAlreadyPresent = true;
                return attachment;
            }

            attachment = creator();
            pack.Attach(attachment);
            wasAlreadyPresent = false;
            return attachment;
        }
    }
}
