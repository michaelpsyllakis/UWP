using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Editing;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.Storage.Streams;

namespace TeleprompterApp.Classes
{
    public class SyntheticVoice
    {
        public double Delay { get; set; }
        public double Pitch { get; set; }
        public double Rate { get; set; }
        public double Volume { get; set; }
        public string LanguageTag { get; set; }
        public string VoiceDisplayName { get; set; }
        public string Text { get; set; }
        public bool UseSyntheticVoice { get; set; }
        public bool HasLanguage { get; set; }
        public List<(string, double)> SyntheticVoicInfo { get; set; }
        public List<BackgroundAudioTrack> Tracks { get; set; }
        public BackgroundAudioTrack Track { get; set; }

        public SyntheticVoice()
        {
            Delay = 0;
            Pitch = 1.0;
            Rate = 1.0;
            Volume = 0.8;
            LanguageTag = "";
            VoiceDisplayName = "";
            Text = "";
            UseSyntheticVoice = false;
            HasLanguage = false;
            Tracks = new List<BackgroundAudioTrack>();
        }

        public void SetDelay(double delay)
        {
            Delay = delay;
        }

        public void SetLanguage(string languageTag)
        {
            LanguageTag = languageTag.Trim();
            HasLanguage = LanguageTag.Length > 0;
        }

        public async Task SetAudioFile(StorageFile audioFile)
        {
            if (audioFile != null)
            {
                Track = await BackgroundAudioTrack.CreateFromFileAsync(audioFile);
            }
        }

        public void SetSyntheticVoiceInfo(List<(string, double)> captionInfo)
        {
            SyntheticVoicInfo = new List<(string, double)>(captionInfo);
        }

        public async Task PrepareAudioTracks()
        {
            VoiceInformation voice = SpeechSynthesizer.AllVoices.FirstOrDefault(x => x.Language == LanguageTag && x.DisplayName == VoiceDisplayName);
            if (voice == null)
            {
                return;
            }
            if (SyntheticVoicInfo == null)
            {
                return;
            }

            SpeechSynthesizer synthesizer = new SpeechSynthesizer
            {
                Voice = voice
            };
            synthesizer.Options.AudioVolume = Volume;
            synthesizer.Options.AudioPitch = Pitch;
            synthesizer.Options.SpeakingRate = Rate;
            synthesizer.Options.IncludeWordBoundaryMetadata = true;
            synthesizer.Options.IncludeSentenceBoundaryMetadata = true;
            synthesizer.Options.PunctuationSilence = SpeechPunctuationSilence.Default;
            synthesizer.Options.AppendedSilence = SpeechAppendedSilence.Default;

            for (int i = 0; i < SyntheticVoicInfo.Count; i++)
            {
                string text = SyntheticVoicInfo[i].Item1;
                double trackDelay = SyntheticVoicInfo[i].Item2 + 0;
                using (SpeechSynthesisStream synthesisStream = await synthesizer.SynthesizeTextToStreamAsync(text))
                {
                    if (synthesisStream != null)
                    {
                        if (synthesisStream.Size > 0)
                        {
                            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("AudioFile.mp3", CreationCollisionOption.GenerateUniqueName);
                            FileManager.AddFile(file);
                            using (var reader = new DataReader(synthesisStream))
                            {
                                await reader.LoadAsync((uint)synthesisStream.Size);
                                IBuffer buffer = reader.ReadBuffer((uint)synthesisStream.Size);
                                await FileIO.WriteBufferAsync(file, buffer);
                            }
                            if (file != null)
                            {
                                BackgroundAudioTrack track = await BackgroundAudioTrack.CreateFromFileAsync(file);
                                track.Delay = TimeSpan.FromMilliseconds(Delay + trackDelay);
                                Tracks.Add(track);
                            }
                        }
                    }
                }
            }
        }

        public BackgroundAudioTrack GetAudioTrack()
        {
            Track.Delay = TimeSpan.FromMilliseconds(Delay);
            return Track;
        }
    }
}
