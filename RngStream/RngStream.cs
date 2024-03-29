using System;
using System.Text;

namespace RngStreams
{
    /// <summary> 
    /// Title:          RngStream (based on original RngStream.java class)
    /// Description:    Multiple Streams and Substreams of Random Numbers
    /// Copyright:      Pierre L'Ecuyer, University of Montreal
    /// Notice:         This code can be used freely for personal, academic,
    ///                 or non-commercial purposes. For commercial purposes,
    ///                 please contact P. L'Ecuyer at: lecuyer@iro.UMontreal.ca
    /// Version         1.0
    /// Date:           14 August 2001
    ///
    /// Original C# version: 1.1
    /// Author Original C# version: Pim Flik, Free University of Amsterdam, the Netherlands
    /// Date: 05 November 2008
    /// 
    /// Modified C# version: 1.5
    /// Author Modified C# version: David Ball, Queen's University, Kingson, ON, Canada
    /// Date: 08 February 2010
    /// </summary>
    [Serializable]
    public class RngStream
    {
        #region Static Members

        /// <summary>
        /// Default seed of the package and seed for the next stream to be created.
        /// </summary>
        private static double[] nextSeed = { 12345, 12345, 12345, 12345, 12345, 12345 };

        /// <summary>
        /// The currently set package seed (store for posterity mostly!!).
        /// </summary>
        private static long[] packageSeed = { 12345, 12345, 12345, 12345, 12345, 12345 };

        /// <summary>
        /// The cuurent "original" seed - that is the seed value specified by the user or generated from the
        /// the clock (stored for posterity, as above)
        /// </summary>
        private static long originalSeed = 12345;

        /// <summary>
        /// Counter used to assign a numeric identifier to each stream.  First stream is stream 0.
        /// Counter is reset to 0 whenever a new originalSeed is defined
        /// </summary>
        private static int nextStreamID = 0;

        /// <summary>
        /// Boolean value indicating whether or not to raise exceptions if problems occur.
        /// If set to false, any problems are reported to the console
        /// </summary>
        public static bool RaiseError
        {
            get
            {
                return _raiseError;
            }
            set
            {
                _raiseError = value;
            }

        }
        private static bool _raiseError = false;

        /// <summary>
        /// Locking object so that seeds are only set one at a time
        /// </summary>
        private static object seedLocker = new object();

        /// <summary>
        /// Get a long integer vector containing the currently defined package seed
        /// </summary>
        /// <returns></returns>
        public static long[] PackageSeed
        {
            get
            {
                lock (seedLocker)
                {
                    // make a copy to ensure that accidental changes do not happen
                    long[] retValue = new long[6];
                    for (int i = 0; i < 6; i++) { retValue[i] = packageSeed[i]; }
                    return retValue;
                }
            }
        }

        /// <summary>
        /// Get the original seed value as set by the user or set by the clock.  If the seed was set by directly
        /// specifying a vector of six longs, the original seed is set to -1 (see the getPackageSeed method above to
        /// get this initial seed value).
        /// </summary>
        /// <returns>The original seed value</returns>
        public static long OriginalSeed
        {
            get
            {
                lock (seedLocker)
                {
                    return originalSeed;
                }
            }
        }

        /// <summary>
        /// Sets the initial seed of the first stream to a vector consisting of six longs set to a single value derived
        /// from the current system time. This will be the seed (initial state) of the first stream. If one of the setPackageSeed
        /// methods is not called, the default initial seed is (12345, 12345, 12345, 12345, 12345, 12345). The passed seed value
        /// must be less than m2 = 4294944443, and not 0.
        /// </summary>
        /// <param name="seed">The seed value</param>
        /// <returns>True if the seeds are successfully set, false otherwise.</returns>
        public static bool setPackageSeed()
        {
            return setPackageSeed(DateTime.Now.Ticks & 4294944443);
        }

        /// <summary>
        /// Sets the initial seed of the first stream to a vector consisting of six longs set to a single passed
        /// value. This will be the seed (initial state) of the first stream. If one of the setPackageSeed methods is not
        /// called, the default initial seed is (12345, 12345, 12345, 12345, 12345, 12345). The passed seed value must be
        /// less than m2 = 4294944443, and not 0.
        /// </summary>
        /// <param name="seed">The seed value</param>
        /// <returns>True if the seeds are successfully set, false otherwise.</returns>
        public static bool setPackageSeed(long seed)
        {
            lock (seedLocker)
            {
                long[] seedVals = new long[6];
                for (int i = 0; i < 6; i++) seedVals[i] = seed;
                bool retValue = setPackageSeed(seedVals);
                originalSeed = seed;
                nextStreamID = 0;
                return retValue;
            }
        }

        /// <summary>
        /// Sets the initial seed of the first stream to the six longs in the passed vector seed. This will
        /// be the seed (initial state) of the first stream. If one of the setPackageSeed methods is not called, the 
        /// default initial seed is (12345, 12345, 12345, 12345, 12345, 12345). If it is called, the first 3
        /// values of the seed must all be less than m1 = 4294967087, and not all 0; and the last 3 values must 
        /// all be less than m2 = 4294944443, and not all 0.  NOTE: if this method is called, the originalSeed value
        /// is set to -1.
        /// </summary>
        /// <param name="seed">The list of seeds to set.</param>
        /// <returns>True if the seeds are successfully set, false otherwise.</returns>
        public static bool setPackageSeed(long[] seed)
        {
            lock (seedLocker)
            {
                originalSeed = -1;
                // Must use long because there is no unsigned int type.
                if (RngStream.CheckSeed(seed) != 0)
                    return false;     // FAILURE
                for (int i = 0; i < 6; ++i)
                {
                    nextSeed[i] = seed[i];
                    packageSeed[i] = seed[i];
                }
                nextStreamID = 0;
                return true;         // SUCCESS
            }
        }

        /// <summary>
        /// Check the current seed.  Print an error meesage or throw an exception if the seed value is
        /// not acceptable.
        /// </summary>
        /// <param name="seed">The list of seeds to check</param>
        /// <returns>0 if the seed is OK, -1 if there is a problem</returns>
        private static int CheckSeed(long[] seed)
        {
            /* Check that the seeds are legitimate values. Returns 0 if legal seeds,
             * -1 otherwise. */
            int i;

            for (i = 0; i < 3; ++i)
            {
                if (seed[i] >= m1 || seed[i] < 0)
                {
                    if (RaiseError)
                    {
                        throw new RngStreamException(string.Format("Seed[{0}], Seed is not set.", i));
                    }
                    else
                    {
                        Console.Out.WriteLine(string.Format("****************************************\n" + "ERROR: Seed[{0}],   Seed is not set." + "\n****************************************\n", i));
                        return -1;
                    }
                }
            }
            for (i = 3; i < 6; ++i)
            {
                if (seed[i] >= m2 || seed[i] < 0)
                {
                    if (RaiseError)
                    {
                        throw new RngStreamException(string.Format("Seed[{0}], Seed is not set.", i));
                    }
                    else
                    {
                        Console.Out.WriteLine(string.Format("****************************************\n" + "ERROR: Seed[{0}],   Seed is not set." + "\n****************************************\n", i));
                        return -1;
                    }

                }
            }
            if (seed[0] == 0 && seed[1] == 0 && seed[2] == 0)
            {
                if (RaiseError)
                {
                    throw new RngStreamException("First 3 seeds = 0.");
                }
                else
                {
                    Console.Out.WriteLine("****************************\n" + "ERROR: First 3 seeds = 0.\n" + "****************************\n");
                    return -1;
                }
            }
            if (seed[3] == 0 && seed[4] == 0 && seed[5] == 0)
            {
                if (RaiseError)
                {
                    throw new RngStreamException("Last 3 seeds = 0.");
                }
                else
                {
                    Console.Out.WriteLine("****************************\n" + "ERROR: Last 3 seeds = 0.\n" + "****************************\n");
                    return -1;
                }
            }
            return 0;
        }

        #endregion

        #region Private Constants

        // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        // Private constants.
        private const double norm = 2.328306549295727688e-10;
        private const double m1 = 4294967087.0;
        private const double m2 = 4294944443.0;
        private const double a12 = 1403580.0;
        private const double a13n = 810728.0;
        private const double a21 = 527612.0;
        private const double a23n = 1370589.0;
        private const double two17 = 131072.0;
        private const double two53 = 9007199254740992.0;
        private const double invtwo24 = 5.9604644775390625e-8;

        private static readonly double[][] InvA1 = { new double[] { 184888585.0, 0.0, 1945170933.0 }, new double[] { 1.0, 0.0, 0.0 }, new double[] { 0.0, 1.0, 0.0 } };
        private static readonly double[][] InvA2 = { new double[] { 0.0, 360363334.0, 4225571728.0 }, new double[] { 1.0, 0.0, 0.0 }, new double[] { 0.0, 1.0, 0.0 } };
        private static readonly double[][] A1p0 = { new double[] { 0.0, 1.0, 0.0 }, new double[] { 0.0, 0.0, 1.0 }, new double[] { -810728.0, 1403580.0, 0.0 } };
        private static readonly double[][] A2p0 = { new double[] { 0.0, 1.0, 0.0 }, new double[] { 0.0, 0.0, 1.0 }, new double[] { -1370589.0, 0.0, 527612.0 } };
        private static readonly double[][] A1p76 = { new double[] { 82758667.0, 1871391091.0, 4127413238.0 }, new double[] { 3672831523.0, 69195019.0, 1871391091.0 }, new double[] { 3672091415.0, 3528743235.0, 69195019.0 } };
        private static readonly double[][] A2p76 = { new double[] { 1511326704.0, 3759209742.0, 1610795712.0 }, new double[] { 4292754251.0, 1511326704.0, 3889917532.0 }, new double[] { 3859662829.0, 4292754251.0, 3708466080.0 } };
        private static readonly double[][] A1p127 = { new double[] { 2427906178.0, 3580155704.0, 949770784.0 }, new double[] { 226153695.0, 1230515664.0, 3580155704.0 }, new double[] { 1988835001.0, 986791581.0, 1230515664.0 } };
        private static readonly double[][] A2p127 = { new double[] { 1464411153.0, 277697599.0, 1610723613.0 }, new double[] { 32183930.0, 1464411153.0, 1022607788.0 }, new double[] { 2824425944.0, 32183930.0, 2093834863.0 } };

        #endregion

        #region Private Variables for Each Stream

        // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        // Private variables (fields) for each stream.

        // The arrays {\tt Cg}, {\tt Bg}, and {\tt Ig} contain the current state,
        // the starting point of the current substream,
        // and the starting point of the stream, respectively.
        private double[] Cg;
        private double[] Bg;
        private double[] Ig;

        /// <summary>
        /// The precision of the output numbers is ``increased'' (see
        /// {\tt increasedPrecis}) if and only if {\tt prec53 = true}.
        /// </summary>
        private bool prec53;

        /// <summary>
        /// Describes the stream (for writing the state, error messages, etc.).
        /// </summary>
        private string descriptor;

        private void InitBlock()
        {
            Ig = new double[6];
            Bg = new double[6];
            Cg = new double[6];
        }

        /// <summary>
        /// The numeric ID of the stream. 
        /// </summary>
        private int streamID = -1;

        /// <summary>
        /// The assigned seed of the stream generator used to create this instance
        /// </summary>
        private long streamSeed = -1;

        #endregion

        #region Private Methods

        /// <summary>
        /// Compute (a*s + c) MOD m ; m must be < 2^35
        /// Works also for s, c < 0.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        private static double multModM(double a, double s, double c, double m)
        {
            double v;
            int a1;

            v = a * s + c;
            if (v >= two53 || v <= -two53)
            {
                a1 = (int)(a / two17);
                a -= a1 * two17;
                v = a1 * s;

                a1 = (int)(v / m);
                v -= a1 * m;
                v = v * two17 + a * s + c;
            }

            a1 = (int)(v / m);
            if ((v -= a1 * m) < 0.0)
                return v += m;
            else
                return v;
        }

        /// <summary>
        /// Returns v = A*s MOD m.  Assumes that -m < s[i] < m.
        /// Works even if v = s.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="s"></param>
        /// <param name="v"></param>
        /// <param name="m"></param>
        private static void matVecModM(double[][] A, double[] s, double[] v, double m)
        {
            int i;

            double[] x = new double[3];
            for (i = 0; i < 3; ++i)
            {
                x[i] = multModM(A[i][0], s[0], 0.0, m);
                x[i] = multModM(A[i][1], s[1], x[i], m);
                x[i] = multModM(A[i][2], s[2], x[i], m);
            }
            for (i = 0; i < 3; ++i)
                v[i] = x[i];
        }

        /// <summary>
        /// Returns C = A*B MOD m */
        /// Note: works even if A = C or B = C or A = B = C.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <param name="m"></param>
        private static void matMatModM(double[][] A, double[][] B, double[][] C, double m)
        {
            int i, j;

            double[] V = new double[3];
            double[][] W = new double[3][];
            for (int i2 = 0; i2 < 3; i2++)
            {
                W[i2] = new double[3];
            }
            for (i = 0; i < 3; ++i)
            {
                for (j = 0; j < 3; ++j)
                    V[j] = B[j][i];
                matVecModM(A, V, V, m);
                for (j = 0; j < 3; ++j)
                    W[j][i] = V[j];
            }
            for (i = 0; i < 3; ++i)
            {
                for (j = 0; j < 3; ++j)
                    C[i][j] = W[i][j];
            }
        }

        /// <summary>
        /// Compute matrix B = (A^(2^e) Mod m);  works even if A = B
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="m"></param>
        /// <param name="e"></param>
        private static void matTwoPowModM(double[][] A, double[][] B, double m, int e)
        {
            int i, j;

            /* initialize: B = A */
            if (A != B)
            {
                for (i = 0; i < 3; i++)
                {
                    for (j = 0; j < 3; ++j)
                        B[i][j] = A[i][j];
                }
            }
            /* Compute B = A^{2^e} */
            for (i = 0; i < e; i++)
                matMatModM(B, B, B, m);
        }

        /// <summary>
        /// Compute matrix D = A^c Mod m ;  works even if A = B
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="m"></param>
        /// <param name="c"></param>
        private static void matPowModM(double[][] A, double[][] B, double m, int c)
        {
            int i, j;
            int n = c;

            double[][] W = new double[3][];
            for (int i2 = 0; i2 < 3; i2++)
            {
                W[i2] = new double[3];
            }

            /* initialize: W = A; B = I */
            for (i = 0; i < 3; i++)
            {
                for (j = 0; j < 3; ++j)
                {
                    W[i][j] = A[i][j];
                    B[i][j] = 0.0;
                }
            }
            for (j = 0; j < 3; ++j)
                B[j][j] = 1.0;

            /* Compute B = A^c mod m using the binary decomp. of c */
            while (n > 0)
            {
                if ((n % 2) == 1)
                    matMatModM(W, B, B, m);
                matMatModM(W, W, W, m);
                n /= 2;
            }
        }

        /// <summary>
        /// Generate a uniform random number, with 32 bits of resolution.
        /// </summary>
        /// <returns>A uniform random number, with 32 bits of resolution.</returns>
        private double U01()
        {
            int k;
            double p1, p2, u;

            /* Component 1 */
            p1 = a12 * Cg[1] - a13n * Cg[0];
            k = (int)(p1 / m1);
            p1 -= k * m1;
            if (p1 < 0.0)
                p1 += m1;
            Cg[0] = Cg[1]; Cg[1] = Cg[2]; Cg[2] = p1;
            /* Component 2 */
            p2 = a21 * Cg[5] - a23n * Cg[3];
            k = (int)(p2 / m2);
            p2 -= k * m2;
            if (p2 < 0.0)
                p2 += m2;
            Cg[3] = Cg[4]; Cg[4] = Cg[5]; Cg[5] = p2;
            /* Combination */
            u = ((p1 > p2) ? (p1 - p2) * norm : (p1 - p2 + m1) * norm);
            return (anti) ? (1 - u) : u;
        }

        /// <summary>
        /// Generate a uniform random number, with 52 bits of resolution.
        /// </summary>
        /// <returns>A uniform random number, with 52 bits of resolution.</returns>
        private double U01d()
        {
            double u = U01();

            if (anti)
            {
                // Antithetic case: note that U01 already returns 1-u.
                u += (U01() - 1.0) * invtwo24;
                return (u < 0.0) ? u + 1.0 : u;
            }
            else
            {
                u += U01() * invtwo24;
                return (u < 1.0) ? u : (u - 1.0);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new stream. It initializes its seed Ig, sets Bg and Cg equal to Ig,
        /// and sets its antithetic and precision switches to false. The seed Ig is equal 
        /// to the initial seed of the package given by SetPackageSeed method if this is the
        /// first stream created, otherwise it is Z steps ahead of that of the most recently
        /// created stream.
        /// </summary>
        public RngStream()
        {
            lock (seedLocker)
            {
                InitBlock();
                anti = false;
                prec53 = false;
                for (int i = 0; i < 6; ++i)
                    Bg[i] = Cg[i] = Ig[i] = nextSeed[i];
                matVecModM(A1p127, nextSeed, nextSeed, m1);
                double[] temp = new double[3];
                for (int i = 0; i < 3; ++i)
                    temp[i] = nextSeed[i + 3];
                matVecModM(A2p127, temp, temp, m2);
                for (int i = 0; i < 3; ++i)
                    nextSeed[i + 3] = temp[i];
                // assign the package seed
                streamSeed = originalSeed;
                // assign ID and increment the nextStreamID
                streamID = nextStreamID;
                nextStreamID++;
                // now create the initial state string
                _initialState = string.Format("{0}, {1}, {2}, {3}, {4}, {5}", Cg[0], Cg[1], Cg[2], Cg[3], Cg[4], Cg[5]);
            }
        }

        /// <summary>
        /// Creates a new stream with a specified identifier. It initializes its seed Ig, 
        /// sets Bg and Cg equal to Ig, and sets its antithetic and precision switches to
        /// false. The seed Ig is equal to the initial seed of the package given by SetPackageSeed
        /// method if this is the first stream created, otherwise it is Z steps ahead of that of
        /// the most recently created stream.
        /// </summary>
        /// <param name="name">The identifier of the newly created stream</param>
        public RngStream(string name)
            : this()
        {
            descriptor = name;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Get the assigned package seed (see the setPackageStream method) of the stream generator used to 
        /// create this instance 
        /// </summary>
        public long StreamSeed
        {
            get
            {
                return streamSeed;
            }
        }

        /// <summary>
        /// Get the numeric ID assigned to this instance.  This is a unique ID based on when the stream was created
        /// after the package seed is set (see the setPackageStream method).  The first stream created after the
        /// package seed is set is assigned StreamID = 0
        /// </summary>
        public int StreamID
        {
            get
            {
                return streamID;
            }
        }

        /// <summary>
        /// Get or set a boolean value indicating whether or not this stream returns Antithetic
        /// values (1-U as opposed to U).  Can be set and reset at any time.
        /// </summary>
        public virtual bool Antithetic
        {
            get
            {
                return anti;
            }
            set
            {
                anti = value;
            }
        }     
        private bool anti = false;

        /// <summary>
        /// Get the current state Cg of the stream. This is convenient if we want to save the
        /// state for subsequent use
        /// </summary>
        public virtual double[] State
        {
            get
            {
                return Cg;
            }
        }

        /// <summary>
        /// Get a string describing the initial state of the random number generator when it was created
        /// </summary>
        public virtual string InitialState
        {
            get
            {
                return (_initialState == null ? "" : _initialState);
            }
        }
        private string _initialState;

        /// <summary>
        /// Get or set a boolean value indicating whether or not to return high precison values.
        /// If set to true, each call for a random deviate will advance the state of the stream 
        /// by 2 steps instead of 1, and will return a number with (roughly) 53 bits of precision
        /// instead of 32 bits. More specifically, in the non-antithetic case, the instruction
        /// “x = this.RandU01()” when the precision is increased is equivalent to
        /// “x = (this.RandU01() + this.RandU01() * fact) % 1.0” where the constant fact is equal
        /// to 2−24. This also applies with indirect calls (e.g., by calling the RandInt method). By
        /// default, or if this property is set to false, each call for a random deviate advances the
        /// state by 1 step and returns a number with 32 bits of precision.  This property can be changed
        /// multiple times during the life time of the stream.
        /// </summary>
        public bool HighPrecision
        {
            get
            {
                return prec53;
            }
            set
            {
                prec53 = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the initial seed Ig of stream g to the vector seed. This vector must satisfy the same
        /// conditions as the SetPackageSeed method. The stream is then reset to this initial seed. The
        /// states and seeds of the other streams are not modified. As a result, after calling this method,
        /// the initial seeds of the streams are no longer spaced Z values apart. We discourage the use of
        /// this method. Returns false for invalid seeds, true otherwise.  StreamID is set to -1 after this
        /// method is called.
        /// </summary>
        /// <param name="seed">The seed values to set.</param>
        /// <returns>True if the seed values are successfully set, false otherwise.</returns>
        public virtual bool setSeed(long[] seed)
        {
            int i;

            if (RngStream.CheckSeed(seed) != 0)
                return false;    // FAILURE
            for (i = 0; i < 6; ++i)
                Cg[i] = Bg[i] = Ig[i] = seed[i];
            streamID = -1;
            return true;         // SUCCESS
        }

        /// <summary>
        /// Reinitializes the stream to the beginning of its current substream.
        /// </summary>
        public virtual void resetStartStream()
        {
            for (int i = 0; i < 6; ++i)
                Cg[i] = Bg[i] = Ig[i];
        }

        /// <summary>
        /// Reinitializes the stream to the beginning of its next substream.
        /// </summary>
        public virtual void resetStartSubstream()
        {
            for (int i = 0; i < 6; ++i)
                Cg[i] = Bg[i];
        }

        /// <summary>
        /// Reset the next substream
        /// </summary>
        public virtual void resetNextSubstream()
        {
            int i;

            matVecModM(A1p76, Bg, Bg, m1);
            double[] temp = new double[3];
            for (i = 0; i < 3; ++i)
                temp[i] = Bg[i + 3];
            matVecModM(A2p76, temp, temp, m2);
            for (i = 0; i < 3; ++i)
                Bg[i + 3] = temp[i];
            for (i = 0; i < 6; ++i)
                Cg[i] = Bg[i];
        }

        /// <summary>
        /// ???
        /// </summary>
        /// <param name="e"></param>
        /// <param name="c"></param>
        public virtual void advanceState(int e, int c)
        {
            double[][] B1 = new double[3][], C1 = new double[3][];
            for (int i = 0; i < 3; i++)
            {
                B1[i] = new double[3];
            }
            for (int i2 = 0; i2 < 3; i2++)
            {
                C1[i2] = new double[3];
            }
            double[][] B2 = new double[3][], C2 = new double[3][];
            for (int i3 = 0; i3 < 3; i3++)
            {
                B2[i3] = new double[3];
            }
            for (int i4 = 0; i4 < 3; i4++)
            {
                C2[i4] = new double[3];
            }

            if (e > 0)
            {
                matTwoPowModM(A1p0, B1, m1, e);
                matTwoPowModM(A2p0, B2, m2, e);
            }
            else if (e < 0)
            {
                matTwoPowModM(InvA1, B1, m1, -e);
                matTwoPowModM(InvA2, B2, m2, -e);
            }

            if (c >= 0)
            {
                matPowModM(A1p0, C1, m1, c);
                matPowModM(A2p0, C2, m2, c);
            }
            else if (c < 0)
            {
                matPowModM(InvA1, C1, m1, -c);
                matPowModM(InvA2, C2, m2, -c);
            }

            if (e != 0)
            {
                matMatModM(B1, C1, C1, m1);
                matMatModM(B2, C2, C2, m2);
            }

            matVecModM(C1, Cg, Cg, m1);
            double[] cg3 = new double[3];
            for (int i = 0; i < 3; i++)
                cg3[i] = Cg[i + 3];
            matVecModM(C2, cg3, cg3, m2);
            for (int i = 0; i < 3; i++)
                Cg[i + 3] = cg3[i];
        }

        /// <summary>
        /// Output the current state of the of the stream to the console
        /// </summary>
        public virtual void writeState()
        {
            Console.Write(string.Format("{0}\n", this.getState()));
        }

        /// <summary>
        /// Return a description of the current state of the of the stream.
        /// </summary>
        /// <returns>A string containing the current state of the current stream.</returns>
        public virtual string getState()
        {
            StringBuilder sb = new StringBuilder("The current state of the RngStream");
            if (!string.IsNullOrEmpty(descriptor)) sb.AppendFormat(" {0}", descriptor);
            sb.Append(":\n    Cg = { ");
            for (int i = 0; i < 5; i++)
            {
                sb.AppendFormat("{0}, ", (long)Cg[i]);
            }
            sb.AppendFormat("{0} }}", (long)Cg[5]);
            return sb.ToString();
        }

        /// <summary>
        /// Return a string descriptor of this instance (stream)
        /// </summary>
        /// <returns>A string description of the instance (the state)</returns>
        public override string ToString()
        {
            return getState();
        }

        /// <summary>
        /// Output the full current state of the of the random generator to the console
        /// </summary>
        public virtual void writeStateFull()
        {
            Console.Write(string.Format("{0}\n", this.getStateFull()));
        }

        /// <summary>
        /// Return a description of the full current state of the of the stream.
        /// </summary>
        /// <returns>A string containing the description of the full current state of the of the stream.</returns>
        public virtual string getStateFull()
        {
            StringBuilder sb = new StringBuilder("The RngStream");
            if (!string.IsNullOrEmpty(descriptor)) sb.AppendFormat("{0} ", descriptor);
            sb.AppendFormat(":\n   Antithetic: {0}" + (anti ? "true" : "false"));
            sb.AppendFormat("\n   Precision: {0}" + (prec53 ? "53 bits" : "32 bits"));

            sb.Append("\n   Ig = { ");
            for (int i = 0; i < 5; i++)
            {
                sb.AppendFormat("{0}, ", (long)Ig[i]);
            }
            sb.AppendFormat("{0} }}", (long)Ig[5]);

            sb.Append("\n   Bg = { ");
            for (int i = 0; i < 5; i++)
            {
                sb.AppendFormat("{0}, ", (long)Bg[i]);
            }
            sb.AppendFormat("{0} }}", (long)Bg[5]);

            sb.Append("\n   Cg = { ");
            for (int i = 0; i < 5; i++)
            {
                sb.AppendFormat("{0}, ", (long)Cg[i]);
            }
            sb.AppendFormat("{0} }}", (long)Cg[5]);

            return sb.ToString();
        }

        /// <summary>
        /// Return a uniform random number (double) in the range 0 to 1.
        /// </summary>
        /// <returns>a uniform random number (double) in the range 0 to 1.</returns>
        public virtual double randU01()
        {
            try
            {
                if (prec53)
                    return this.U01d();
                else
                    return this.U01();
            }
            catch (Exception ex)
            {
                // throw a RngStreamException, passing the original exception as the inner exception
                throw new RngStreamException(string.Format("Error occured generating a RngStream random number. {0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// Return a uniform random integer within a specified range
        /// </summary>
        /// <param name="Lower">The lower range value</param>
        /// <param name="Upper">The upper range value</param>
        /// <returns>A random number in the range lower to upper</returns>
        public virtual int randInt(int Lower, int Upper)
        {
            return (Lower + (int)(randU01() * (Upper - Lower + 1.0)));
        }

        #endregion
    }
}