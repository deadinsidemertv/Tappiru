using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TappiruCS.Core.GameObject;
using TappiruCS.Render.Text;
using TappiruCS.State.Edit.Core;
using TappiruCS.State.Edit.TimelineSystem;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Edit.Panels
{
    public class InfoPanel
    {
        private readonly Scene _scene;
        private readonly EditState _editState;

        private Container infoContaner;

        private InputField _previveTimeInputField;
        private InputField _endTimeInputField;
        public InfoPanel(Scene scene, EditState editState)
        {
            _scene = scene;
            _editState = editState;
        }

        public void Show()
        {

            Hide();
            infoContaner = new Container(160, 450);
            

            var _titletext = new TextObject("Title", -150, -30, 36f);
            _titletext.FontKey = "Game";
            _titletext.Align = TextAlign.Left;
            _titletext.Color = "#919bb8";
            var _titleInputField = new InputField(0, 0, 300, 40);
            _titleInputField.Text = _editState.title;

            _titleInputField.OnTextChanged += OnTitleChanged;

            var _artisttext = new TextObject("Artist", -150, 70, 36f);
            _artisttext.FontKey = "Game";
            _artisttext.Align = TextAlign.Left;
            _artisttext.Color = "#919bb8";
            var _artistinputfield = new InputField(0, 100, 300, 40);
            _artistinputfield.Text = _editState.artist;

            _artistinputfield.OnTextChanged += OnArtistChanged;

            var _previewtimetext = new TextObject("Preview Time", -150, 170, 36f);
            _previewtimetext.FontKey = "Game";
            _previewtimetext.Align = TextAlign.Left;
            _previewtimetext.Color = "#919bb8";
            _previveTimeInputField = new InputField(0, 200, 120, 40);
            _previveTimeInputField.PlaceHolderText = "...";
            _previveTimeInputField.Text = _editState.previewTime.ToString();
            _previveTimeInputField.OnTextChanged += OnPreviewTimeChanged;


            var _endtimetext = new TextObject("End Time", -150, 270, 36f);
            _endtimetext.FontKey = "Game";
            _endtimetext.Align = TextAlign.Left;
            _endtimetext.Color = "#919bb8";
            _endTimeInputField = new InputField(0, 300, 120, 40);
            _endTimeInputField.PlaceHolderText = "...";
            _endTimeInputField.Text = _editState.endTime.ToString();
            _endTimeInputField.OnTextChanged += OnEndTimeChanged;




            infoContaner.AddChild(_titletext);
            infoContaner.AddChild(_artisttext);
            infoContaner.AddChild(_previewtimetext);
            infoContaner.AddChild(_endtimetext);


            infoContaner.AddChild(_titleInputField);
            infoContaner.AddChild(_artistinputfield);

            infoContaner.AddChild(_previveTimeInputField);
            infoContaner.AddChild(_endTimeInputField);

            infoContaner.RecalculateSize();


            _scene.Add(infoContaner);
        }


        public void Hide()
        {
            if(infoContaner!= null)
            {
                _scene.Remove(infoContaner);
                infoContaner = null;
            }
              
        }


        public void OnTitleChanged(string value)
        {
            _editState?.title = value;
        }

        public void OnArtistChanged(string value)
        {
            _editState?.artist = value;
        }

        public void OnPreviewTimeChanged(string value)
        {
            if (double.TryParse(_previveTimeInputField.Text, CultureInfo.InvariantCulture, out double result))
                _editState.previewTime = result;
        }

        public void OnEndTimeChanged(string value)
        {
            if (double.TryParse(_endTimeInputField.Text, CultureInfo.InvariantCulture, out double result))
                _editState.endTime = result;
        }
    }



}

