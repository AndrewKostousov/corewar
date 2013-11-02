﻿using System.Collections.Generic;
using System.IO;
using Core.Parser;

namespace Core.Engine
{
    public class Engine
    {
        public List<RunningWarrior> Warriors { get; private set; }
        public readonly Memory Memory;
        public int CurrentWarrior { get; private set; }
        public int CurrentStep { get; private set; }
        public int CurrentIp { get; private set; }
        public bool GameOver { get; private set; }
        public int? Winner { get; private set; }

        private StepResult stepResult;

        public Engine(IEnumerable<WarriorStartInfo> warriorsStartInfos)
        {
            Memory = new Memory(Parameters.CORESIZE);
            Warriors = new List<RunningWarrior>();
            var idx = 0;
            foreach (var wsi in warriorsStartInfos)
            {
                var warrior = new RunningWarrior(wsi.Warrior, idx++, wsi.LoadAddress);
                Warriors.Add(warrior);
                PlaceWarrior(warrior, wsi.LoadAddress);
            }
            CurrentWarrior = 0;
            CurrentStep = 0;
        }

        private void PlaceWarrior(RunningWarrior warrior, int address)
        {
            var statements = warrior.Warrior.Statements;
            for (var i = 0; i < statements.Count; ++i)
                Memory[address + i] = new Instruction(statements[i], ModularArith.Mod(address + i), warrior.Index);
        }

        public StepResult Step()
        {
            if (GameOver)
                return new StepResult();

            CurrentIp = Warriors[CurrentWarrior].Queue.Dequeue();
            var instruction = Memory[CurrentIp];

            stepResult = new StepResult();

            ExecuteInstruction(instruction);
            
            if (! stepResult.KilledInInstruction)
                Warriors[CurrentWarrior].Queue.Enqueue(stepResult.SetNextIP.HasValue ? stepResult.SetNextIP.GetValueOrDefault() : ModularArith.Mod(CurrentIp + 1));

            if (stepResult.SplittedInInstruction.HasValue)
                Warriors[CurrentWarrior].Queue.Enqueue(stepResult.SplittedInInstruction.GetValueOrDefault());

            var nextWarrior = GetNextWarrior(CurrentWarrior);
            if (! nextWarrior.HasValue)
            {
                GameOver = true;
                Winner = CurrentWarrior;
                return stepResult;
            }
            CurrentWarrior = nextWarrior.GetValueOrDefault();
            CurrentStep++;

            return stepResult;
        }

        private int? GetNextWarrior(int currentWarrior)
        {
            var startWarrior = currentWarrior;
            currentWarrior = (currentWarrior + 1) % Warriors.Count;
            if (Warriors[currentWarrior].Queue.Count == 0)
            {
                while (currentWarrior != startWarrior && Warriors[currentWarrior].Queue.Count == 0)
                    currentWarrior = (currentWarrior + 1) % Warriors.Count;
                if (currentWarrior == startWarrior)
                    return null;
            }
            return currentWarrior;
        }

        private void ExecuteInstruction(Instruction instruction)
        {
            new InstructionExecutor(instruction).Execute(this);
        }

        public void WriteToMemory(int address, Statement statement)
        {
            Memory[address].Statement = statement;
            Memory[address].LastModifiedByProgram = CurrentWarrior;
            stepResult.ChangeMemory(address, statement, CurrentWarrior);
        }

        public void KillCurrentProcess()
        {
            stepResult.KilledInInstruction = true;
        }

        public void JumpTo(int address)
        {
            stepResult.SetNextIP = address;
        }

        public void SplitAt(int address)
        {
            stepResult.SplittedInInstruction = address;
        }
    }
}
