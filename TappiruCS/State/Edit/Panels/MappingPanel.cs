using System;
using System.Collections.Generic;
using TappiruCS.Core.GameObject;
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

        public MappingPanel(Scene scene)
        {
            _scene = scene;
        }

        public void Show(ITimelineSelectable selected)
        {
            // Если выбран не Phrase, скрываем панель
            if (selected is not Phrase phrase)
            {
                Hide();
                return;
            }

            // Если та же самая фраза уже открыта, ничего не делаем
            if (_currentPhrase == phrase)
                return;

            // Удаляем старую панель, если она есть
            Hide();

            // Запоминаем текущую фразу
            _currentPhrase = phrase;

            // Создаём новый скролл-контейнер
            _scrollContainer = new ScrollContainer(50, 300, 400, 600,20) { Layer = 10 };
            int length = _currentPhrase.Text.Length;
            for (int i = 0; i < length; i++)
            {
                AddMappingRow(_currentPhrase.Text[i]);
            }

            _scene.Add(_scrollContainer);
        }

        private void AddMappingRow(char ch)
        {
            Container mappingCell = new Container(0, 0);
            TextObject charLabel = new TextObject(ch.ToString(), 0, 0, 36f);
            InputField lengthInput = new InputField(150, 0, 100, 35);
            lengthInput.PlaceHolderText = "len";
            mappingCell.AddChild(lengthInput);
            mappingCell.AddChild(charLabel);

            _scrollContainer!.AddItem(mappingCell);
            _scrollContainer.RecalcMaxScroll();
        }

        public void Hide()
        {
            if (_scrollContainer != null)
            {
                _scene.Remove(_scrollContainer);
                _scrollContainer = null;
            }
            _currentPhrase = null;
        }
    }
}