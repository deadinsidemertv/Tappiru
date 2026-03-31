using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using OpenTK.Audio.OpenAL;
using NAudio;

namespace TappiruCS.GameLogic
{
    public class GameSession
    {
        //-------------Гейм механика--------------------//
        public MapData CurrentMap; //Данные карты
        private int currentStringIndex = 0; //Индекс строки
        public string stringPhase; //Сама строка
        public int currentCharIndex = 0 ; //индекс чара который нужно нажать
        private bool isActivePhase = false; //Активна ли фаза
        public double currentTime;
        public char[] charArray; //строка разбитая на символы
        private double newPhaseStartTime;
        public bool PhaseComplete=false;

        public double endTime;

        public  int currentKeyDownIndex;
        //------------Игровые показатели---------------//
        public int totalScore; //Общий скор
        public int combo; //комбо
        public float health;

        public GameSession(MapData mapData)
        {
            
            CurrentMap = mapData;
            this.totalScore = 0;
            this.combo = 0;
            this.health = 100;

            endTime = mapData.endTime;
        }


        public void Update(double currentTime)
        {
            if (!isActivePhase && currentStringIndex < CurrentMap.Events.Count)
            {
                
                
                var ev = CurrentMap.Events[currentStringIndex];
                if (currentStringIndex + 1 < CurrentMap.Events.Count)
                {
                    newPhaseStartTime = CurrentMap.Events[currentStringIndex + 1].time;
                }
                if (currentTime >= ev.time && currentTime<newPhaseStartTime)
                {
                    
                    stringPhase = ev.text;
                    charArray = stringPhase.ToCharArray(); //Разбиваем строку на чары
                    currentCharIndex = 0;
                    isActivePhase = true;
                    PhaseComplete = false;

                }
  
            }
            if (currentTime >= newPhaseStartTime)
            {
                
                isActivePhase = false;
                currentStringIndex++;
                currentCharIndex = 0;
                if (currentStringIndex + 1 < CurrentMap.Events.Count)
                {
                    newPhaseStartTime = CurrentMap.Events[currentStringIndex + 1].time;
                }
                PhaseComplete = false;
            }

            

        }

        public void HandleInput(char c)
        {
            if (isActivePhase)
            {
                if (c == charArray[currentCharIndex])
                {
                    currentCharIndex++;
                    combo++;
                    totalScore +=  300*combo;
                }
                else
                {
                    Console.WriteLine("Ошибка");
                    combo = 0;
                }
                if (currentCharIndex == charArray.Length)
                {
                    PhaseComplete = true;
                    isActivePhase = false;
                    currentStringIndex++;
                    currentCharIndex = 0;
                }
            }
            
        }

        


    }
}
