﻿using Core.Parser;

namespace Core.Engine
{
    public class RunningWarrior
    {
        public Warrior Warrior { get; private set; }

        public int Index { get; private set; }

        public readonly RunningQueue Queue;
    	public uint? LastPointer;

    	public RunningWarrior(Warrior warrior, int index, int loadAddress, int coresize)
        {
            Warrior = warrior;
            Index = index;
            Queue = new RunningQueue(coresize);
            Queue.Enqueue(loadAddress + warrior.StartAddress);
        }
    }
}
