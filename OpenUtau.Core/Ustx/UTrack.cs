﻿using System;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Render;
using Serilog;
using YamlDotNet.Serialization;

namespace OpenUtau.Core.Ustx {
    public class UTrack {
        public string singer;
        public string phonemizer;
        public string renderer;

        private USinger singer_;

        [YamlIgnore]
        public USinger Singer {
            get => singer_;
            set {
                if (singer_ != value) {
                    singer_ = value;
                    Phonemizer.SetSinger(value);
                    VoiceColorExp = null;
                }
            }
        }
        [YamlIgnore] public Phonemizer Phonemizer { get; set; } = PhonemizerFactory.Get(typeof(DefaultPhonemizer)).Create();
        [YamlIgnore] public string PhonemizerTag => Phonemizer.Tag;
        [YamlIgnore] public IRenderer Renderer { get; set; }

        [YamlIgnore] public string SingerName => Singer != null ? Singer.DisplayName : "[No Singer]";
        [YamlIgnore] public int TrackNo { set; get; }
        [YamlIgnore] public int DisplayTrackNo => TrackNo + 1;
        public bool Mute { set; get; }
        public bool Solo { set; get; }
        public double Volume { set; get; }
        [YamlIgnore] public double Pan { set; get; }
        [YamlIgnore] public UExpressionDescriptor VoiceColorExp { set; get; }

        public bool TryGetExpression(UProject project, string key, out UExpressionDescriptor descriptor) {
            if (!project.expressions.TryGetValue(key, out descriptor)) {
                return false;
            }
            if (key == Format.Ustx.CLR && VoiceColorExp != null) {
                descriptor = VoiceColorExp;
            }
            return true;
        }

        public void OnSingerRefreshed() {
            if (Singer != null && Singer.Loaded && !SingerManager.Inst.Singers.ContainsKey(Singer.Id)) {
                Singer = USinger.CreateMissing(Singer.Name);
            }
            VoiceColorExp = null;
        }

        public void Validate(ValidateOptions options, UProject project) {
            if (Singer != null && Singer.Found) {
                Singer.EnsureLoaded();
            }
            if (project.expressions.TryGetValue(Format.Ustx.CLR, out var descriptor)) {
                if (VoiceColorExp == null && Singer != null && Singer.Found && Singer.Loaded) {
                    VoiceColorExp = descriptor.Clone();
                    var colors = Singer.Subbanks.Select(subbank => subbank.Color).ToHashSet();
                    VoiceColorExp.options = colors.OrderBy(c => c).ToArray();
                    VoiceColorExp.max = VoiceColorExp.options.Length - 1;
                }
            }
        }

        public void BeforeSave() {
            singer = Singer?.Id;
            phonemizer = Phonemizer.GetType().FullName;
            renderer = Renderer?.ToString();
        }

        public void AfterLoad(UProject project) {
            if (Phonemizer == null || !string.IsNullOrEmpty(phonemizer)) {
                try {
                    var factory = DocManager.Inst.PhonemizerFactories.FirstOrDefault(factory => factory.type.FullName == phonemizer);
                    Phonemizer = factory?.Create();
                    phonemizer = null;
                } catch (Exception e) {
                    Log.Error(e, $"Failed to load phonemizer {phonemizer}");
                }
            }
            if (Phonemizer == null) {
                Phonemizer = PhonemizerFactory.Get(typeof(DefaultPhonemizer)).Create();
            }
            if (Singer == null && !string.IsNullOrEmpty(singer)) {
                Singer = SingerManager.Inst.GetSinger(singer);
                if (Singer == null) {
                    Singer = USinger.CreateMissing(singer);
                }
            }
            Phonemizer.SetSinger(Singer);
            if (Singer != null && Singer.Found) {
                Renderer = Renderers.CreateRenderer(renderer);
                if (Renderer == null) {
                    renderer = Renderers.GetDefaultRenderer(Singer.SingerType);
                    Renderer = Renderers.CreateRenderer(renderer);
                }
            }
            TrackNo = project.tracks.IndexOf(this);
        }
    }
}
