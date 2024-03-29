using System;
using Random;
using System.Threading;
using System.Data;
using Rabies_Model_Core;
using System.Collections.Generic;

namespace Rabies_Model_Core
{

    /// <summary>
    ///		Defines the arguments for the two events raised by the background class every
    ///		week.
    ///	</summary>
    public class cWeeklyUpdateEventArgs : EventArgs
    {
        // ********************** Constructor **************************************
        /// <summary>
        ///		Initialize.
        /// </summary>
        /// <param name="Background">
        ///		The background against which this event is being raised.  An ArgumentNullException
        ///		exception is raised in Background is null.
        /// </param>
        public cWeeklyUpdateEventArgs(cBackground Background)
        {
            if (Background == null)
                throw new ArgumentNullException("Background", "Background must not be null.");
            // set the parameters
            mvarYear = Background.Years.CurrentYearNum;
            mvarWeek = Background.Years.CurrentYear.CurrentWeek;
            mvarBackground = Background;
            mvarAbort = false;
        }

        // ******************** Properties *****************************************
        /// <summary>
        ///		The year for which this event is being raised.
        /// </summary>
        public int Year
        {
            get
            {
                return mvarYear;
            }
        }

        /// <summary>
        ///		The week for which this event is being raised.
        /// </summary>
        public int Week
        {
            get
            {
                return mvarWeek;
            }
        }

        /// <summary>
        ///     The background for which this event is being raised
        /// </summary>
        public cBackground Background
        {
            get
            {
                return mvarBackground;
            }
        }

        /// <summary>
        ///		A flag indicating that the run should abort
        /// </summary>
        public bool Abort
        {
            get
            {
                return mvarAbort;
            }
            set
            {
                mvarAbort = value;
            }
        }

        // ******************** Private members ************************************
        private cBackground mvarBackground;
        private int mvarYear;
        private int mvarWeek;
        private bool mvarAbort;
    }

    /// <summary>
    ///		Delegate event handler for the WeeklyUpdateBeforeRemovingDead event generated by the
    ///		cBackground	class.
    /// </summary>
    public delegate void WeeklyUpdateBeforeRemovingDeadEventHandler(object sender,
                                                                    cWeeklyUpdateEventArgs e);

    /// <summary>
    ///		Delegate event handler for the WeeklyUpdateAfterRemovingDead event generated by the
    ///		cBackground	class.
    /// </summary>
    public delegate void WeeklyUpdateAfterRemovingDeadEventHandler(object sender,
                                                                    cWeeklyUpdateEventArgs e);

    /// <summary>
    ///		The parent object of the rabies model.  This object has several roles in the model: 
    ///		Firstly, it acts as a container for the master cell list and master animal list
    ///		(i.e. the list of all of the cells and all of the animals in the model) and the
    ///		list of the years through which the model will run.  It also holds a list of
    ///		Diseases that can affect the animals living in this background.  
    ///		Secondly, it directs the running of the model by incrementing the time and
    ///		calling appropriate action methods each week for each animal.
    ///		Lastly, it sends messages to its owner class, allowing that class to display and
    ///		store weekly information about the progress of the model as it runs.
    /// </summary>
    public class cBackground
    {
        /// <summary>
        ///		Initialize the background.  Defaults to an initial 10 year time period
        ///		with a normal winter bias.
        /// </summary>
        /// <param name="Rnd">The random number generator to be used by the background</param>
        /// <param name="Name">
        ///		The name to assign to this background.  An ArgumentException is raised if
        ///		Name is zero length.
        ///	</param>
        /// <param name="KeepAllAnimals">
        ///		A flag indicating whether a record of all animals should be kept during
        ///		a run.
        ///	</param>
        public cBackground(cUniformRandom Rnd, string Name, bool KeepAllAnimals)
            : this(Rnd, Name, KeepAllAnimals, 10, enumWinterType.Normal)
        {
        }

        /// <summary>
        ///		Initialize the background specifying the initial number of years.  Years 
        ///		are created with a normal winter bias.
        /// </summary>
        /// <param name="Rnd">The random number generator to be used by the background</param>
        /// <param name="Name">
        ///		The name to assign to this background.  An ArgumentException is raised if
        ///		Name is zero length.
        ///	</param>
        /// <param name="KeepAllAnimals">
        ///		A flag indicating whether a record of all animals should be kept during
        ///		a run.
        ///	</param>
        /// <param name="NYears">
        ///		The initial number of years. An ArgumentException is raised in NYears is
        ///		less than or equal to zero.
        ///	</param>
        public cBackground(cUniformRandom Rnd, string Name, bool KeepAllAnimals, int NYears)
            : this(Rnd, Name, KeepAllAnimals, NYears, enumWinterType.Normal)
        {
        }

        /// <summary>
        ///		Initialize the background specifying the initial number of years and their
        ///		winter bias.
        /// </summary>
        /// <param name="Rnd">The random number generator to be used by the background</param>
        /// <param name="Name">
        ///		The name to assign to this background.  An ArgumentException is raised if
        ///		Name is zero length.
        ///	</param>
        /// <param name="KeepAllAnimals">
        ///		A flag indicating whether a record of all animals should be kept during
        ///		a run.
        ///	</param>
        /// <param name="NYears">
        ///		The initial number of years. An ArgumentException is raised in NYears is
        ///		less than or equal to zero.
        ///	</param>
        /// <param name="WinterBias">The winter bias of the initial years.</param>
        public cBackground(cUniformRandom Rnd, string Name, bool KeepAllAnimals, int NYears, enumWinterType WinterBias)
        {
            if (Rnd == null) throw new ArgumentNullException("Rnd");
            // reference the random number generator
            RandomNum = Rnd;
            RandomNum.MinValue = 0;
            RandomNum.MaxValue = 100;
            // make sure that Name is not zero length
            if (string.IsNullOrEmpty(Name))
                throw new ArgumentException("Name must not be zero length.", "Name");
            // make sure NYears > 0
            if (NYears <= 0)
                throw new ArgumentException("NYears must be greater than 0.", "NYears");
            // copy the name
            this.Name = Name;
            // create a super cell list
            SuperCells = new cSuperCellList();
            // create a master cell list
            Cells = new cMasterCellList(this, RandomNum);
            // create list of years
            Years = new cYearList(NYears, RandomNum, WinterBias);
            // create the master list of animals
            Animals = new cMasterAnimalList(1, KeepAllAnimals, RandomNum);
            // create the list of diseases
            Diseases = new cDiseaseList(RandomNum);
            // create the strategy list
            Strategies = new cStrategyList();
            StrategyCounter = 0;
            // set have run weekly events to false
            mvarHaveRunWeeklyEvents = false;
            // scramble list is false by default
            ScrambleList = false;
            // abort on disease disappearance is true by default
            AbortOnDiseaseDisappearance = true;
            // prevent incest flag - set to false by default
            PreventIncest = false;
        }

        /// <summary>
        ///		Initialize the background specifying numbers of years and their winter
        ///		types in a winter type list.
        /// </summary>
        /// <param name="Rnd">The random number generator to be used by the background</param>
        /// <param name="Name">
        ///		The name to assign to this background.  An ArgumentException is raised if
        ///		Name is zero length.
        ///	</param>
        /// <param name="KeepAllAnimals">
        ///		A flag indicating whether a record of all animals should be kept during
        ///		a run.
        ///	</param>
        /// <param name="WTList">
        ///		The list of winter types.  An ArgumentException exception is raised if
        ///		WTList is empty.
        ///	</param>
        public cBackground(cUniformRandom Rnd, string Name, bool KeepAllAnimals, cWinterTypeList WTList)
        {
            if (Rnd == null) throw new ArgumentNullException("Rnd");
            if (WTList == null) throw new ArgumentNullException("WTList");

            // reference the random number generator
            RandomNum = Rnd;
            RandomNum.MinValue = 0;
            RandomNum.MaxValue = 100;
            // make sure that Name is not zero length
            if (string.IsNullOrEmpty(Name))
                throw new ArgumentException("Name must not be zero length.", "Name");
            // copy the name
            this.Name = Name;
            // create a super cell list
            SuperCells = new cSuperCellList();
            // create a master cell list
            Cells = new cMasterCellList(this, RandomNum);
            // generate winter and animal lists
            Years = new cYearList(WTList, RandomNum);
            // create the master list of animals
            Animals = new cMasterAnimalList(1, KeepAllAnimals, RandomNum);
            // create the list of diseases
            Diseases = new cDiseaseList(RandomNum);
            // create the strategy list
            Strategies = new cStrategyList();
            StrategyCounter = 0;
            // set have run weekly events to false
            mvarHaveRunWeeklyEvents = false;
            // scramble list is false by default
            ScrambleList = false;
            // abort on disease disappearance is true by default
            AbortOnDiseaseDisappearance = true;
            // prevent incest flag - set to false by default
            PreventIncest = false;
        }

        /// <summary>
        ///		Initialize the background, passing a list of years and a seed for all 
        ///		random number generators in the model.  This is an internal constructor
        ///		and may only be used by classes within the Rabies_Model_Core namespace.
        /// </summary>
        /// <param name="Rnd">The random number generator to be used by the background</param>
        /// <param name="Name">
        ///		The name to assign to this background.  An ArgumentException is raised if
        ///		Name is zero length.
        ///	</param>
        /// <param name="KeepAllAnimals">
        ///		A flag indicating whether a record of all animals should be kept during
        ///		a run.
        ///	</param>
        /// <param name="YList">
        ///		The list of years.  An ArgumentException exception is raised if YList is empty.
        ///	</param>
        ///	<param name="DList">
        ///		A list of diseases that will affect the animals in this background.
        ///	</param>
        public cBackground(cUniformRandom Rnd, string Name, bool KeepAllAnimals, cYearList YList, cDiseaseList DList)
        {
            if (Rnd == null) throw new ArgumentNullException("Rnd");
            if (YList == null) throw new ArgumentNullException("YList");
            if (DList == null) throw new ArgumentNullException("DList");

            // make sure that Name is not zero length
            if (string.IsNullOrEmpty(Name))
                throw new ArgumentException("Name must not be zero length.", "Name");
            // make sure the Years list contains at least one year
            if (YList.Count == 0)
                throw new ArgumentException("Year list must contain at least one year.",
                                            "Years");
            // create the random number generator
            RandomNum = Rnd;
            RandomNum.MinValue = 0;
            RandomNum.MaxValue = 100;
            // copy the name
            this.Name = Name;
            // create a super cell list
            SuperCells = new cSuperCellList();
            // create a master cell list
            Cells = new cMasterCellList(this, RandomNum);
            // generate winter and animal lists
            Years = YList;
            Years.YearRnd = RandomNum;
            // create the master list of animals
            Animals = new cMasterAnimalList(1, KeepAllAnimals, RandomNum);
            // create the list of diseases
            Diseases = DList;
            // create the strategy list
            Strategies = new cStrategyList();
            StrategyCounter = 0;
            // set have run weekly events to false
            mvarHaveRunWeeklyEvents = false;
            // scramble list is false by default
            ScrambleList = false;
            // abort on disease disappearance is true by default
            AbortOnDiseaseDisappearance = true;
            // prevent incest flag - set to false by default
            PreventIncest = false;
        }

        // *************************** Properties ****************************************
        /// <summary>
        ///		The name of this background.
        /// </summary>
        public string Name;
        /// <summary>
        ///		The master list of cells for this background.
        /// </summary>
        public cMasterCellList Cells;
        /// <summary>
        ///		The list of supercells in this background.
        /// </summary>
        public cSuperCellList SuperCells;
        /// <summary>
        ///		The master list of animals for this background.
        /// </summary>
        public cMasterAnimalList Animals;
        /// <summary>
        ///		A random generator of uniform deviates for general use.
        /// </summary>
        public cUniformRandom RandomNum;
        /// <summary>
        ///		A list of years to keep track of time in the model.
        /// </summary>
        public cYearList Years;
        /// <summary>
        ///		A list of diseases that can affect the animals in this background.
        /// </summary>
        public cDiseaseList Diseases;
        /// <summary>
        ///		A list of strategies that can be applied to this backround.
        /// </summary>
        public cStrategyList Strategies;

        /// <summary>
        ///		A flag indicating whether or not the order of the animals in the main animal
        ///		list should be scrambled after each week.
        /// </summary>
        public bool ScrambleList;

        /// <summary>
        ///		A flag indicating whether or not a run of the model should abort if disease first
        ///		appears and then disapppears
        /// </summary>
        public bool AbortOnDiseaseDisappearance;

        /// <summary>
        ///		A flag indicating whether or not a run of the model should use reflective disease spread
        /// </summary>
        public bool UseReflectiveDiseaseSpread;

        // a flag indicating whether the mating system should prevent incest
        public bool PreventIncest;

        /// <summary>
        ///		A flag indicating whther or not the weekly events have been run for the
        ///		current year and week setting (read-only).
        /// </summary>
        public bool HaveRunWeeklyEvents
        {
            get
            {
                return mvarHaveRunWeeklyEvents;
            }
        }

        ///<summary>
        /// //YM
        /// A list of mortality rate of male 
        /// </summary>
        public double[] MaleMortality;
        ///<summary>
        /// //YM
        /// A list of mortality rate of male 
        /// </summary>
        public double[] FemaleMortality;

        // *************************** Methods *******************************************
        /// <summary>
        ///		Runs the model.  Each week, the following actions take place (in this order!!).
        ///		1) The DoWeeklyActivieties function is called for all animals in the model.
        ///		2) Any strategies that should be applied during this week are applied.
        ///		3) The WeeklyUpdate virtual function of this class is called.
        ///		4) The order of the animals in the MasterAnimalList is scrambled.
        ///		5) Time is incremented by a week.  Note: the model stops running when the last
        ///		   week	of the last year is reached.  
        /// </summary>
        public void RunModel()
        {
            //System.Diagnostics.Debug.WriteLine("cBackground.cs: RunModel()");
            bool DiseaseCurrent;
            // sort the strategies list
            Strategies.Sort();
            // start at the first strategy
            StrategyCounter = 0;
            // attach an event handler for the AnimalInfected event to each animal
            Animals.SetAnimalInfectedHandler(new AnimalInfectedEventHandler(this.Animal_AnimalInfected));
            //Console.WriteLine("year; week; gender; age ; nb total female/male ; taux mortalite annuelle; taux mortalite hebdo ; nb de willdie");

            // loop through years
            do
            {
                // check for disease (only done at the beginning of each year)
                // EER: if there is no disease, end the model, if the user requested ending the model when disease was gone
                if (AbortOnDiseaseDisappearance)
                {

                    // note: disease present is a private property set to true when an animal
                    // is infected

                    if (DiseasePresent)
                    {
                        if (Years.CurrentYear.CurrentWeek == 1)
                        {

                            // check for the presence of disease at this point in time
                            DiseaseCurrent = false;
                            foreach (cAnimal A in this.Animals)
                            {
                                if (A.HasDisease)
                                {
                                    DiseaseCurrent = true;
                                    break;
                                }
                            }
                            // check the current status of disease in the model
                            // if disease is no longer in the model, exit this function
                            if (!DiseaseCurrent) return;
                        }
                    }
                }
                //YM: 
                if (Years.CurrentYear.CurrentWeek == 1)
                {
                    Console.WriteLine("Years :" + Years.CurrentYear);
                }
                // now do the weekly calls
                if (!WeeklyCalls(true)) return;
            } while (!Years.IsLastYear || Years.CurrentYear.CurrentWeek != 52);
            // do one last weekly call for the last week of the last year
            WeeklyCalls(false);
            //System.Diagnostics.Debug.WriteLine("cBackground.cs: RunModel() END");
        }

        // *********************** private members ****************************************

        // private function for the actual weekly calls
        private bool WeeklyCalls(bool IncrementTime)
        {
            //System.Diagnostics.Debug.WriteLine("cBackground.cs: WeeklyCalls()");
            //System.Diagnostics.Debug.WriteLine("cBackground.cs: WeeklyCalls() Animals.Count = " + Animals.Count);

            cWeeklyActivityThread ActivityThread = new cWeeklyActivityThread(0, Animals.Count - 1, Animals,
                                                            Years.CurrentYear.CurrentWeek, PreventIncest);

            ActivityThread.RunWeeklyActivities();

            // add babies to master list, attaching the event handler in each case
            //System.Diagnostics.Debug.WriteLine("cBackground.cs: WeeklyCalls() add babies");
            Animals.AddBabiesToList(new AnimalInfectedEventHandler(this.Animal_AnimalInfected));

            // apply any strategies applicable to this year and week
            if (StrategyCounter < Strategies.Count)
            {
                for (; ; )
                {
                    cStrategy CurrentStrategy = Strategies[StrategyCounter];
                    // if the most current strategy should be applied later, exit now
                    if (CurrentStrategy.Year > Years.CurrentYearNum || CurrentStrategy.Week > Years.CurrentYear.CurrentWeek)
                    {
                        break;
                    }
                    // if the most current strategy should be applied now, apply it!!
                    else if (CurrentStrategy.Year == Years.CurrentYearNum && CurrentStrategy.Week == Years.CurrentYear.CurrentWeek)
                    {
                        CurrentStrategy.ApplyStrategy();
                    }
                    // increment the StrategyCounter
                    StrategyCounter++;
                    // make sure we have not just run the last strategy
                    if (StrategyCounter >= Strategies.Count) break;
                }
            }
            // indicate that weekly events have been run
            //System.Diagnostics.Debug.WriteLine("cBackground.cs: WeeklyCalls() indicate that weekly events have been run");
            mvarHaveRunWeeklyEvents = true;

            // raise the WeeklyUpdate event before the dead are removed
            //System.Diagnostics.Debug.WriteLine("cBackground.cs: WeeklyCalls(): raise weekly event before dead are removed");
            cWeeklyUpdateEventArgs e = new cWeeklyUpdateEventArgs(this);
            onWeeklyUpdate(e, false);

            //if (e.Abort) return false;
            //System.Diagnostics.Debug.WriteLine("cBackground.cs: WeeklyCalls(): e.Abort = " + e.Abort);
            if (e.Abort)
            {
                return false;
            }
            // remove dead animals from main list
            Animals.RemoveDeceased();

            // raise weekly event after dead are removed
            //System.Diagnostics.Debug.WriteLine("cBackground.cs: WeeklyCalls(): raise weekly event AFTER dead are removed");
            onWeeklyUpdate(e, true);
            if (e.Abort) return false;
            // scramble the animals
            if (ScrambleList) Animals.ScrambleOrder();
            // increment time
            if (IncrementTime)
            {
                Years.IncrementTime();
                // indicate that weekly events have not been run
                mvarHaveRunWeeklyEvents = false;
            }
            //System.Diagnostics.Debug.WriteLine("cBackground.cs: WeeklyCalls() END");
            return true;
        }

        // ******************** To string function **************************************

        /// <summary>
        /// Get the string representing this object
        /// </summary>
        /// <returns>The string representing this object</returns>
        public override string ToString()
        {
            return this.Name;
        }


        // *********************** Event Handlers ******************************************
        // event handler for detecting animal infection
        private void Animal_AnimalInfected(object sender, cAnimalInfectedEventArgs e)
        {
            DiseasePresent = true;
        }

        // ********************** Private Members *******************************************
        // flag indicating whether weekly run has been made for current year and week setting
        private bool mvarHaveRunWeeklyEvents;

        // counter for accessing strategies one-by-one through the strategy list
        private int StrategyCounter;

        // a flag that starts out false but becomes true if disease ever appears in the model
        bool DiseasePresent = false;

        // ********************* Events ***********************************************
        /// <summary>
        ///		A weekly update event.  This is raised every week after the animals 
        ///		have a chance to do their weekly activities but before any dead animals are
        ///		removed.  The current year and	week are passed back as arguments.
        /// </summary>
        public event WeeklyUpdateBeforeRemovingDeadEventHandler WeeklyUpdateBeforeRemovingDead;

        /// <summary>        	
        ///     A weekly update event.  This is raised every week after the animals 
        ///		have a chance to do their weekly activities and after the dead animals are
        ///		removed.  The current year and	week are passed back as arguments.
        /// </summary>
        public event WeeklyUpdateAfterRemovingDeadEventHandler WeeklyUpdateAfterRemovingDead;

        /// <summary>
        ///		The protected onWeeklyUpdate method raises the event by invoking the
        ///		delegates.  The sender is always this, the current instance of the class.
        /// </summary>
        /// <param name="e">Event arguments for the WeeklyUpdate event.</param>
        protected virtual void onWeeklyUpdate(cWeeklyUpdateEventArgs e, bool DeadRemoved)
        {
            if (DeadRemoved)
            {
                if (WeeklyUpdateAfterRemovingDead != null)
                {
                    // invoke the delegate
                    WeeklyUpdateAfterRemovingDead(this, e);
                }
            }
            else
            {
                if (WeeklyUpdateBeforeRemovingDead != null)
                {
                    // invoke the delegate
                    WeeklyUpdateBeforeRemovingDead(this, e);
                }
            }
        }

        /// <summary>
        /// //YM: 
        /// Select the animal that will die in the current week according their age and the annual mortality rate 
        /// </summary>
        public void SelectWillDieAnimal()
        {
            // Female 
           cAnimalList FemaleAnimal = new cAnimalList(null);
            FemaleAnimal = this.Animals.GetByGender(enumGender.female);
            //Console.WriteLine("nb female :" + FemaleAnimal.Count);
            // create the list of animals with the same age
            for (int i = 0; i <= 7; i++)
            {

                //int count = 0;
                cAnimalList ListAnimalAge = new cAnimalList(null);
                foreach (cAnimal anim in FemaleAnimal)
                {
                    
                    int AgeInYears = Convert.ToInt32(Math.Floor((double)anim.Age / 52));
                    //Console.WriteLine("AgeInYears {0} et i {1}", AgeInYears, i);
                    if (AgeInYears == i)
                    {
                        ListAnimalAge.Add(anim);
                        //FemaleAnimal.Remove(anim.ID);
                        //count++;
                    }
                }
                //Console.WriteLine("nb female {0} ans : {1}, count {2}", i, ListAnimalAge.Count, count);
                //Select randomly the will-die-animal in the ListAnimalAge
                int CountAnimalWillDiePrecise = Convert.ToInt32(Math.Floor(ListAnimalAge.Count * FemaleMortality[i] / 52));
                int MaxValue = Convert.ToInt32(Math.Floor(CountAnimalWillDiePrecise + CountAnimalWillDiePrecise * 0.05));
                int MinValue = Convert.ToInt32(Math.Floor(CountAnimalWillDiePrecise - CountAnimalWillDiePrecise * 0.05));
                // Select a number close to the count of will-die animals 
                int CountAnimalWillDie = this.RandomNum.IntValue(MaxValue, MinValue);
                // For each age, select randomly the list of will-die animal
                for (int j = 0; j < CountAnimalWillDie; j++)
                {
                    int RanNum = this.RandomNum.IntValue(0, CountAnimalWillDie - 1);
                    if (ListAnimalAge[RanNum].WillDie)

                        if (ListAnimalAge[RanNum].WillDie)
                        {
                            do
                            {
                                RanNum = this.RandomNum.IntValue(0, CountAnimalWillDie - 1);
                            } while (!ListAnimalAge[RanNum].WillDie);
                            ListAnimalAge[RanNum].WillDie = true;
                        }
                        else
                        {
                            ListAnimalAge[RanNum].WillDie = true;
                        }
                    //ListAnimalAge[j].WillDie = true;
                }
                //Console.WriteLine("{0};{1};{2};{3};{4};{5};{6};{7}", Years.CurrentYearNum, Years.CurrentYear.CurrentWeek, enumGender.female, i, ListAnimalAge.Count, FemaleMortality[i], FemaleMortality[i]/52, CountAnimalWillDie);

            }



            // Male
           cAnimalList MaleAnimal = new cAnimalList(null);
            MaleAnimal = this.Animals.GetByGender(enumGender.male);
            // create the list of animals with the same age
            //Console.WriteLine("nb male :" + MaleAnimal.Count);
            for (int i = 0; i <= 7; i++)
            {
                cAnimalList ListAnimalAge = new cAnimalList(null);
                foreach (cAnimal anim in MaleAnimal)
                {
                    int AgeInYears = Convert.ToInt32(Math.Floor((double)anim.Age / 52));
                    if (AgeInYears == i)
                    {
                        ListAnimalAge.Add(anim);
                        //MaleAnimal.Remove(anim.ID);
                    }
                }
                //Console.WriteLine("nb male {0} ans : {1}", i, ListAnimalAge.Count);
                //Select randomly the will-die-animal in the ListAnimalAge
                int CountAnimalWillDiePrecise = Convert.ToInt32(Math.Floor(ListAnimalAge.Count * MaleMortality[i] / 52));
                int MaxValue = Convert.ToInt32(Math.Floor(CountAnimalWillDiePrecise + CountAnimalWillDiePrecise * 0.1));
                int MinValue = Convert.ToInt32(Math.Floor(CountAnimalWillDiePrecise - CountAnimalWillDiePrecise * 0.1));
                // Select a number close to the count of will-die animals 
                int CountAnimalWillDie = this.RandomNum.IntValue(MaxValue, MinValue);
                // For each age, select randomly the list of will-die animal
                for (int j = 0; j < CountAnimalWillDie; j++)
                {
                    int RanNum = this.RandomNum.IntValue(0, CountAnimalWillDie - 1);
                    if (ListAnimalAge[RanNum].WillDie)
                    {
                        do
                        {
                            RanNum = this.RandomNum.IntValue(0, CountAnimalWillDie - 1);
                        } while (!ListAnimalAge[RanNum].WillDie);
                        ListAnimalAge[RanNum].WillDie = true;
                    }
                    else
                    {
                        ListAnimalAge[RanNum].WillDie = true;
                    }
                    //ListAnimalAge[j].WillDie = true;
                }
                //Console.WriteLine("{0};{1};{2};{3};{4};{5};{6};{7}", Years.CurrentYearNum, Years.CurrentYear.CurrentWeek, enumGender.male, i, ListAnimalAge.Count, MaleMortality[i], MaleMortality[i] / 52, CountAnimalWillDie);
            }
        }

        public void incrementeAnimalAge()
        {
            foreach (cAnimal an in this.Animals)
            {
                an.Age++;
            }
        }
        /// <summary>
        ///		Private class for creating threads to perform weekly activities 
        /// </summary>
        private class cWeeklyActivityThread
        {
            /// <summary>
            ///		Constructor - set all values
            /// </summary>
            /// <param name="StartAt">The position in the list (EER: of animals) to start running</param>
            /// <param name="EndAt">The position in the list (EER: of animals) to stop running</param>
            /// <param name="Animals">A reference to the list of animals</param>
            /// <param name="CurrentWeek">The current week of the year</param>
            /// <param name="PreventIncest">A flag that prevents siblings from mating if set to true</param>
            public cWeeklyActivityThread(int StartAt, int EndAt, cAnimalList Animals, int CurrentWeek, bool PreventIncest)
            {
                mvarStartAt = StartAt;
                mvarEndAt = EndAt;
                mvarCurrentWeek = CurrentWeek;
                mvarAnimals = Animals;
                mvarPreventIncest = PreventIncest;
            }

            /// <summary>
            ///		Run weekly activities for animals betweem start and end points in the animal list
            /// </summary>
            public void RunWeeklyActivities()
            {
                //System.Diagnostics.Debug.WriteLine("cBackground.cs: RunWeeklyActivities()");
                //System.Diagnostics.Debug.WriteLine("    cBackground.cs: RunWeeklyActivities(): mvarCurrentWeek = " + mvarCurrentWeek);
                
                // loop from start to end
                for (int i = mvarStartAt; i <= mvarEndAt; i++)
                {
                    //System.Diagnostics.Debug.WriteLine("    cBackground.cs: RunWeeklyActivities(): mvarAnimals[i].ID = " + mvarAnimals[i].ID);
                    //Console.WriteLine("REMOVE");
                    //Console.WriteLine("bg count : " + mvarAnimals[i].Background.Animals.Count);
                    //Console.WriteLine("bg adult count : " + mvarAnimals[i].Background.Animals.GetCountByAgeClass(enumAgeClass.Adult));
                    //Console.WriteLine("bg juv count : " + mvarAnimals[i].Background.Animals.GetCountByAgeClass(enumAgeClass.Juvenile));

                    mvarAnimals[i].DoWeeklyActivities(mvarCurrentWeek, mvarPreventIncest);
                    //mvarAnimals[i].Background.Animals.Remove(mvarAnimals[i].ID);

                    //Console.WriteLine("bg count2 : " + mvarAnimals[i].Background.Animals.Count);
                    //Console.WriteLine("bg adult2 count : " + mvarAnimals[i].Background.Animals.GetCountByAgeClass(enumAgeClass.Adult));
                    //Console.WriteLine("bg juv2 count : " + mvarAnimals[i].Background.Animals.GetCountByAgeClass(enumAgeClass.Juvenile));

                }

            }

            // private members - store values passed in constructor
            private int mvarStartAt;
            private int mvarEndAt;
            private int mvarCurrentWeek;
            private cAnimalList mvarAnimals;
            private bool mvarPreventIncest;

        }
    }
}
