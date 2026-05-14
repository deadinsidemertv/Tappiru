using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.UI.Toggle
{
    public class RadioButtonGroup<T>
    {
        private List<(RadioButton Button, T Value)> _items = new();
        private T _currentValue;

        public event Action<T> SelectionChanged;

        public void Add(RadioButton button, T value)
        {
            button.RequestActivation += OnButtonRequested;
            _items.Add((button, value));
        }

        private void OnButtonRequested(Toggle clicked)
        {
            var radio = clicked as RadioButton;
            if (radio == null) return;

            // Находим значение для этой кнопки
            T newValue = default;
            bool found = false;
            foreach (var (btn, val) in _items)
            {
                if (btn == radio)
                {
                    newValue = val;
                    found = true;
                    break;
                }
            }
            if (!found) return;

            // Если уже выбрано это значение – ничего не делаем
            if (Equals(_currentValue, newValue)) return;

            // Выключаем все кнопки (без событий)
            foreach (var (btn, _) in _items)
                btn.SetSelected(false, false);

            // Включаем нажатую
            radio.SetSelected(true, false);
            _currentValue = newValue;

            // Уведомляем подписчиков
            SelectionChanged?.Invoke(newValue);
        }

        // Опционально: установить значение без вызова события (для начальной инициализации)
        public void SetValue(T value, bool raiseEvent = false)
        {
            bool valueChanged = !Equals(_currentValue, value);

            foreach (var (btn, val) in _items)
            {
                bool shouldBeSelected = Equals(val, value);
                btn.SetSelected(shouldBeSelected, false);
            }

            _currentValue = value;

            if (valueChanged && raiseEvent)
                SelectionChanged?.Invoke(value);
        }
    }
}
