using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TappiruCS.Core.GameObject;
using TappiruCS.Render.Text;
using TappiruCS.State.Edit.Core;
using TappiruCS.State.Edit.TimelineSystem;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Edit.Panels
{
    public class MappingPanel
    {
        private readonly Scene _scene;
        private Phrase? _currentPhrase;
        private ScrollContainer? _scrollContainer;
        private readonly Dictionary<InputField, int> _inputToIndex = new();

        private ProgressBar conversion;

        public static readonly Dictionary<char, int> DefaultKanaLengths = new()
        {
            // ==================== Хирагана (основные) ====================
            {'あ', 1}, // a
            {'い', 1}, // i
            {'う', 1}, // u
            {'え', 1}, // e
            {'お', 1}, // o

            {'か', 2}, // ka
            {'き', 2}, // ki
            {'く', 2}, // ku
            {'け', 2}, // ke
            {'こ', 2}, // ko

            {'さ', 2}, // sa
            {'し', 3}, // shi
            {'す', 2}, // su
            {'せ', 2}, // se
            {'そ', 2}, // so

            {'た', 2}, // ta
            {'ち', 3}, // chi
            {'つ', 3}, // tsu
            {'て', 2}, // te
            {'と', 2}, // to

            {'な', 2}, // na
            {'に', 2}, // ni
            {'ぬ', 2}, // nu
            {'ね', 2}, // ne
            {'の', 2}, // no

            {'は', 2}, // ha
            {'ひ', 2}, // hi
            {'ふ', 2}, // fu
            {'へ', 2}, // he
            {'ほ', 2}, // ho

            {'ま', 2}, // ma
            {'み', 2}, // mi
            {'む', 2}, // mu
            {'め', 2}, // me
            {'も', 2}, // mo

            {'や', 2}, // ya
            {'ゆ', 2}, // yu
            {'よ', 2}, // yo

            {'ら', 2}, // ra
            {'り', 2}, // ri
            {'る', 2}, // ru
            {'れ', 2}, // re
            {'ろ', 2}, // ro

            {'わ', 2}, // wa
            {'を', 1}, // wo (в современном японском часто читается как 'o', но стандарт Хэпбёрна сохраняет 'wo')
            {'ん', 1}, // n (перед гласными требует апострофа, но длина как 1 символ)

            // ==================== Хирагана (дакутэн) ====================
            {'が', 2}, // ga
            {'ぎ', 2}, // gi
            {'ぐ', 2}, // gu
            {'げ', 2}, // ge
            {'ご', 2}, // go

            {'ざ', 2}, // za
            {'じ', 2}, // ji
            {'ず', 2}, // zu
            {'ぜ', 2}, // ze
            {'ぞ', 2}, // zo

            {'だ', 2}, // da
            {'ぢ', 2}, // ji 
            {'づ', 2}, // zu 
            {'で', 2}, // de
            {'ど', 2}, // do

            {'ば', 2}, // ba
            {'び', 2}, // bi
            {'ぶ', 2}, // bu
            {'べ', 2}, // be
            {'ぼ', 2}, // bo

            // ==================== Хирагана (хандакутэн) ====================
            {'ぱ', 2}, // pa
            {'ぴ', 2}, // pi
            {'ぷ', 2}, // pu
            {'ぺ', 2}, // pe
            {'ぽ', 2}, // po

            // ==================== Катакана (основные) ====================
            {'ア', 1}, // a
            {'イ', 1}, // i
            {'ウ', 1}, // u
            {'エ', 1}, // e
            {'オ', 1}, // o

            {'カ', 2}, // ka
            {'キ', 2}, // ki
            {'ク', 2}, // ku
            {'ケ', 2}, // ke
            {'コ', 2}, // ko

            {'サ', 2}, // sa
            {'シ', 3}, // shi
            {'ス', 2}, // su
            {'セ', 2}, // se
            {'ソ', 2}, // so

            {'タ', 2}, // ta
            {'チ', 3}, // chi
            {'ツ', 3}, // tsu
            {'テ', 2}, // te
            {'ト', 2}, // to

            {'ナ', 2}, // na
            {'ニ', 2}, // ni
            {'ヌ', 2}, // nu
            {'ネ', 2}, // ne
            {'ノ', 2}, // no

            {'ハ', 2}, // ha
            {'ヒ', 2}, // hi
            {'フ', 2}, // fu
            {'ヘ', 2}, // he
            {'ホ', 2}, // ho

            {'マ', 2}, // ma
            {'ミ', 2}, // mi
            {'ム', 2}, // mu
            {'メ', 2}, // me
            {'モ', 2}, // mo

            {'ヤ', 2}, // ya
            {'ユ', 2}, // yu
            {'ヨ', 2}, // yo

            {'ラ', 2}, // ra
            {'リ', 2}, // ri
            {'ル', 2}, // ru
            {'レ', 2}, // re
            {'ロ', 2}, // ro

            {'ワ', 2}, // wa
            {'ヲ', 1}, // wo
            {'ン', 1}, // n

            // ==================== Катакана (дакутэн) ====================
            {'ガ', 2}, // ga
            {'ギ', 2}, // gi
            {'グ', 2}, // gu
            {'ゲ', 2}, // ge
            {'ゴ', 2}, // go

            {'ザ', 2}, // za
            {'ジ', 2}, // ji
            {'ズ', 2}, // zu
            {'ゼ', 2}, // ze
            {'ゾ', 2}, // zo

            {'ダ', 2}, // da
            {'ヂ', 2}, // ji
            {'ヅ', 2}, // zu
            {'デ', 2}, // de
            {'ド', 2}, // do

            {'バ', 2}, // ba
            {'ビ', 2}, // bi
            {'ブ', 2}, // bu
            {'ベ', 2}, // be
            {'ボ', 2}, // bo

            // ==================== Катакана (хандакутэн) ====================
            {'パ', 2}, // pa
            {'ピ', 2}, // pi
            {'プ', 2}, // pu
            {'ペ', 2}, // pe
            {'ポ', 2}, // po
            {'っ', 1},
        };

        private readonly EditState _editState;
        public MappingPanel(Scene scene, EditState editState)
        {
            _scene = scene;
            _editState = editState;
        }

        public void Show(ITimelineSelectable selected)
        {
            if (selected is not Phrase phrase)
            {
                Hide();
                return;
            }

            if (_currentPhrase == phrase) return;

            Hide();

            _currentPhrase = phrase;

            _scrollContainer = new ScrollContainer(70, 420, 400, 500, 20) { Layer = 10 };
            _scrollContainer.SetZone(100, 250, 300, 520);
            //_scrollContainer.Debug = true;
            

            _inputToIndex.Clear();

            for (int i = 0; i < phrase.Text.Length; i++)
            {
                AddMappingRow(phrase.Text[i], i);
            }


            conversion = new ProgressBar(15, 950, 300, 20);
            conversion.AllowOverMax = true;
            conversion.UseEqualMode = true;
            conversion.ColorEqual = "#00ff00";   // зелёный, когда сумма == MaxValue
            conversion.ColorNotEqual = "#ffa500"; // оранжевый, когда сумма < MaxValue
            conversion.ColorOverMax = "#ff0000";  // красный при превышении


            _scene.Add(conversion);
            _scene.Add(_scrollContainer);

            RecalculateProgressBar();
        }

        private void AddMappingRow(char ch, int index)
        {
            if (_currentPhrase == null) return;

            _currentPhrase.ResizeMappingTo(_currentPhrase.Text.Length);

            Container mappingCell = new Container(0, 0);
            TextObject charLabel = new TextObject(ch.ToString(), 0, 0, 36f) { Align = TextAlign.Left };

            bool isJapanese = IsJapaneseCharacter(ch);
            bool isKana = DefaultKanaLengths.ContainsKey(ch);

            if (isJapanese && isKana)
            {
                // Кана – фиксированная длина из словаря
                int fixedLength = DefaultKanaLengths[ch];
                // Принудительно устанавливаем значение в маппинге (для сохранения)
                _currentPhrase.Mapping[index] = fixedLength;

                TextObject lengthLabel = new TextObject(fixedLength.ToString(), 150, 0, 28f)
                {
                    Color = new Color4(0.8f, 0.8f, 0.2f, 1f) // другой цвет, чтобы пользователь видел, что это авто-значение
                };
                mappingCell.AddChild(lengthLabel);
            }
            else if (isJapanese && !isKana)
            {
                // Кандзи (или другие японские символы не из словаря) – редактируемое поле
                InputField lengthInput = new InputField(150, 0, 100, 35);
                lengthInput.PlaceHolderText = "len";
                lengthInput.Text = _currentPhrase.Mapping[index].ToString();

                int capturedIndex = index;
                lengthInput.OnTextChanged += (newText) =>
                {
                    OnMappingLengthChanged(capturedIndex, newText);
                    _editState?.SaveProject(); // сохраняем сразу, как было
                };

                mappingCell.AddChild(lengthInput);
            }
            else
            {
                // Не-японские символы (латиница, цифры) – длина 0, не редактируется
                _currentPhrase.Mapping[index] = 0;
                TextObject zeroLabel = new TextObject("0", 150, 0, 28f)
                {
                    Color = new Color4(0.5f, 0.5f, 0.5f, 1f)
                };
                mappingCell.AddChild(zeroLabel);
            }

            mappingCell.AddChild(charLabel);
            _scrollContainer!.AddItem(mappingCell);
            _scrollContainer.RecalcMaxScroll();
        }

        // Новый отдельный метод — только для маппинга!
        private void OnMappingLengthChanged(int index, string newText)
        {
            if (_currentPhrase == null) return;
            if (index < 0 || index >= _currentPhrase.Mapping.Count) return;

            if (int.TryParse(newText, out int newValue) && newValue >= 0)
                _currentPhrase.Mapping[index] = newValue;
            else if (string.IsNullOrWhiteSpace(newText))
                _currentPhrase.Mapping[index] = 0;

            RecalculateProgressBar();
            

            // ←←← СРАЗУ СОХРАНЯЕМ ПРОЕКТ
            _editState?.SaveProject();
        }

        private bool IsJapaneseCharacter(char c)
        {
            return (c >= 0x3040 && c <= 0x309F) || // Hiragana
                   (c >= 0x30A0 && c <= 0x30FF) || // Katakana
                   (c >= 0x4E00 && c <= 0x9FFF);   // Kanji
        }

        public void Hide()
        {
            if (_scrollContainer != null)
            {
                _scene.Remove(_scrollContainer);
                _scrollContainer = null;
            }

            _currentPhrase = null;
            _inputToIndex.Clear(); // если используешь

            _scene.Remove(conversion);
            conversion = null!;
        }

        private int GetTranscriptionLengthWithoutSpaces()
        {
            if (_currentPhrase == null) return 0;
            // Убираем все пробелы (не только ' ', но и любые пробельные символы)
            return Regex.Replace(_currentPhrase.Transcription, @"\s+", "").Length;
        }

        private void RecalculateProgressBar()
        {
            int maxLen = GetTranscriptionLengthWithoutSpaces();
            conversion.MaxValue = maxLen > 0 ? maxLen : 1; // защита от деления на 0
            conversion.MinValue = 0;


            int currMappingSum = 0;
            for (int i = 0; i < _currentPhrase.Mapping.Count; i++)
            {
                Console.WriteLine("[" + i + "]" + ": " + _currentPhrase.Mapping[i]);
                currMappingSum += _currentPhrase.Mapping[i];
            }
            conversion.SetValueInstant(currMappingSum);
        }
    }



}

