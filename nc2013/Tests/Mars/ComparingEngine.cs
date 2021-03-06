// This file is part of nMars - Core War MARS for .NET 
// Whole solution including it's license could be found at
// http://sourceforge.net/projects/nmars/
// 2006 Pavel Savara

using System;
using System.Collections.Generic;
using nMars.RedCode;
using nMars.RedCode.Modules;

namespace Tests.Mars
{

    #region Helper Classes

    public class Check
    {
        public Check(int round, int cycle, bool precise)
        {
            if (precise)
            {
                Round = round;
                FromCycle = cycle;
                ToCycle = cycle;
                Step = 1;
            }
            else
            {
                Round = round;
                FromCycle = cycle - 200;
                Step = 200;
                if (FromCycle < 0)
                    FromCycle = 0;
                ToCycle = cycle;
            }
            if (cycle == 0 && Round > 0)
            {
                Round = 0;
            }
        }

        public Check(int round, int fromCycle, int toCycle, int step)
        {
            FromCycle = fromCycle;
            ToCycle = toCycle;
            Step = step;
            Round = round;
        }

        public int FromCycle;
        public int ToCycle;
        public int Step;
        public int Round;
        public static Check Optimistic = new Check(0, int.MaxValue, int.MaxValue, 37777);
    }

    public class EngineDifferException : Exception
    {
        public EngineDifferException(string message, Check check)
            : base(message)
        {
            Check = check;
        }

        public Check Check;
    }

    #endregion

    class ComparingEngine : BaseComponent, IEngine
    {

        public ComparingEngine(IExtendedStepEngine aEngineOne, IExtendedStepEngine aEngineTwo)
        {
            engineOne = aEngineOne;
            engineTwo = aEngineTwo;
        }

        public MatchResult Run(IProject aProject, ISimpleOutput console)
        {
            project = (Project)aProject;
            Init();
            project.EngineOptions.ForcedAddresses = forcedArdresses;
            project.EngineOptions.Random = new Random(0);

            Check check = Check.Optimistic;
#if DEBUG
            // this loop restarts test with more and more precise check setting to isolate bugs in engine
            bool res;
            do
            {
                res = true;
                try
                {
                    CompareEngines(check);
                }
                catch (EngineDifferException e)
                {
                    check = e.Check;
                    res = false;
                }
            } while (!res);
#else
            CompareEngines(check);
#endif
            return matchTwo;
        }

        private void Init()
        {
            project.EngineOptions.Random = new Random(0);
            project.EngineOptions.DumpResults = false;

            forcedArdresses = new List<int>();
            forcedArdresses.Add(0);
            forcedArdresses.Add(4231);
        }

        #region Loop

        private void CompareEngines(Check check)
        {
            engineSbOne = engineOne as IStepBackEngine;
            engineSbTwo = engineTwo as IStepBackEngine;
            engineOne.BeginMatch(project);
            engineTwo.BeginMatch(project);

            try
            {
                CheckWarriors(project.Rules, engineOne.Warriors, engineTwo.Warriors);
                ExpensiveCheck(false);
                while (Step(check))
                {
                }
                ExpensiveCheck(false);
            }
            finally
            {
                matchOne = engineOne.EndMatch(null);
                matchTwo = engineTwo.EndMatch(null);
            }

            if (matchOne != matchTwo)
            {
                throw new EngineDifferException("Score", Check.Optimistic);
            }
        }

        private bool Step(Check check)
        {
            bool stop = (check.Step == 1) && (engineOne.Round == check.Round) && (check.ToCycle <= engineOne.Cycle + 1);
            if (stop)
            {
                // set debuger breakpoint here, this will stop before error ocurs
                ExpensiveCheck(true);
            }
            StepResult resOne = engineOne.NextStep();
            StepResult resTwo = engineTwo.NextStep();
            bool range = (engineOne.Cycle >= check.FromCycle && engineOne.Cycle <= check.ToCycle &&
                          engineOne.Round == check.Round);
            bool step = (check.Step != 1) && (engineOne.Cycle % check.Step == 0) && (engineOne.Round >= check.Round);

            CheapCheck(resOne, resTwo, check, range);
            if (stop || step || range || resOne == StepResult.NextRound)
            {
                ExpensiveCheck(range);
                if (engineSbOne != null)
                {
                    if (engineSbOne.CanStepBack)
                    {
                        engineSbOne.PrevStep();
                        engineOne.NextStep();
                        CheapCheck(resOne, resTwo, check, range);
                    }
                }
                if (engineSbTwo != null)
                {
                    if (engineSbTwo.CanStepBack)
                    {
                        engineSbTwo.PrevStep();
                        engineTwo.NextStep();
                        CheapCheck(resOne, resTwo, check, range);
                    }
                }
                ExpensiveCheck(range);
            }

            if (resOne == StepResult.Finished)
            {
                return false;
            }
            return true;
        }

        public IProject Project
        {
            get { return project; }
        }

        public ISimpleOutput Output
        {
            set { output = value; }
        }

        #endregion

        #region Tests

        private void CheapCheck(StepResult resOne, StepResult resTwo, Check origCheck, bool precise)
        {
            if (resOne != resTwo)
            {
                throw new EngineDifferException("StepRes",
                                                new Check(0, origCheck.FromCycle, origCheck.ToCycle, origCheck.Step / 2));
            }
            if (engineOne.Cycle != engineTwo.Cycle)
            {
                throw new EngineDifferException("Cycle", new Check(engineOne.Round, engineOne.Cycle, precise));
            }
            if (engineOne.Round != engineTwo.Round)
            {
                throw new EngineDifferException("Round", new Check(engineOne.Round, engineOne.Cycle, precise));
            }
            if (engineOne.LiveWarriorsCount != engineTwo.LiveWarriorsCount)
            {
                throw new EngineDifferException("Died", new Check(engineOne.Round, engineOne.Cycle, precise));
            }
            if (engineOne.CyclesLeft != engineTwo.CyclesLeft)
            {
                throw new EngineDifferException("CyclesLeft", new Check(engineOne.Round, engineOne.Cycle, precise));
            }
            if (engineOne.NextWarriorIndex != engineTwo.NextWarriorIndex)
            {
                throw new EngineDifferException("Cheating", new Check(engineOne.Round, engineOne.Cycle, precise));
            }
        }

        private void ExpensiveCheck(bool precise)
        {
            for (int a = 0; a < project.Rules.CoreSize; a++)
            {
                IInstruction iOne = engineOne[a];
                IInstruction iTwo = engineTwo[a];
                if (!iOne.Equals(iTwo))
                {
                    throw new EngineDifferException("Core", new Check(engineOne.Round, engineOne.Cycle, precise));
                }
            }
            IList<IPSpace> psapacesOne = engineOne.PSpaces;
            IList<IPSpace> psapacesTwo = engineTwo.PSpaces;
            for (int w = 0; w < project.Rules.WarriorsCount; w++)
            {
                if (!psapacesOne[w].Equals(psapacesTwo[w]))
                {
                    throw new EngineDifferException("PSpaces", new Check(engineOne.Round, engineOne.Cycle, precise));
                }

                if (engineOne.LastResults[w] != engineTwo.LastResults[w])
                {
                    throw new EngineDifferException("LastResult", new Check(engineOne.Round, engineOne.Cycle, precise));
                }
                if (engineOne.PSPaceIndexes[w] != engineTwo.PSPaceIndexes[w])
                {
                    throw new EngineDifferException("PSPaceIndexes",
                                                    new Check(engineOne.Round, engineOne.Cycle, precise));
                }
                if (engineOne.Warriors[w].Pin != engineTwo.Warriors[w].Pin)
                {
                    throw new EngineDifferException("Pin", new Check(engineOne.Round, engineOne.Cycle, precise));
                }
                IEnumerable<int> tasksOne = engineOne.Tasks[w];
                IEnumerable<int> tasksTwo = engineTwo.Tasks[w];

                IEnumerator<int> enumerator1 = tasksOne.GetEnumerator();
                IEnumerator<int> enumerator2 = tasksTwo.GetEnumerator();

                try
                {
                    while (enumerator1.MoveNext())
                    {
                        if (!enumerator2.MoveNext())
                        {
                            throw new EngineDifferException("Task Died",
                                                            new Check(engineOne.Round, engineOne.Cycle, precise));
                        }
                        if (enumerator1.Current != enumerator2.Current)
                        {
                            throw new EngineDifferException("Bad IP",
                                                            new Check(engineOne.Round, engineOne.Cycle, precise));
                        }
                    }
                    if (enumerator2.MoveNext())
                    {
                        throw new EngineDifferException("Task Died",
                                                        new Check(engineOne.Round, engineOne.Cycle, precise));
                    }
                }
                finally
                {
                    enumerator1.Dispose();
                    enumerator2.Dispose();
                }
            }
        }

        public static void CheckWarriors(Rules rules, IList<IWarrior> warriorsOne, IList<IWarrior> warriorsTwo)
        {
            for (int w = 0; w < rules.WarriorsCount; w++)
            {
                if (warriorsOne[w] == null || warriorsTwo[w] == null)
                    throw new ParserException("Warriror not loaded");

                if (!Warrior.Equals(warriorsOne[w], warriorsTwo[w]))
                {
                    throw new EngineDifferException("Different warriors", new Check(0, 0, false));
                }
            }
        }

        #endregion

        #region Variables

        private IExtendedStepEngine engineOne;
        private IExtendedStepEngine engineTwo;
        private IStepBackEngine engineSbOne;
        private IStepBackEngine engineSbTwo;
        private MatchResult matchOne;
        private MatchResult matchTwo;
        private Project project;
        private List<int> forcedArdresses;
        private ISimpleOutput output;

        #endregion
    }
}