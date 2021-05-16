//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace GridSandbox
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    
    using System.Windows.Controls;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //BEGIN: for EK131 Students -- feel free to change these variables
        const int numGridRows = 7, numGridCols = 7;
        //END: for EK131

        // do not change the following variables
        bool useLeftHand = true;
        const double gridHeight = 600, gridWidth = 600;
        Point playerOneHandLocation, playerTwoHandLocation;

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        /// 
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        bool needsAllocationPlayerOne, needsAllocationPlayerTwo;

        ulong playerOneTrackingId, playerTwoTrackingId;

        System.Windows.Media.Brush playerOneBrush;

        System.Windows.Media.Brush playerTwoBrush;

        /////////////////////
        // Variables to use//
        /////////////////////
        int delayFrames;
        int currentRow;
        int[,] gridArray;
        Rectangle[,] rectArray;
        Image[,] imgArray;
        bool isGameOver, waitingOnBot;
        bool isPlayerOneHandClosed, hasPlayerOneHandOpened;
        bool isPlayerTwoHandClosed, hasPlayerTwoHandOpened;
        int countTime;
        int gameOverDelay;

        ////////////////
        //DO NOT TOUCH//
        ////////////////
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

            //dynamically add a grid      

            uniGrid.Rows = numGridRows; uniGrid.Columns = numGridCols;
            //rows left->right
            rectArray = new Rectangle[numGridRows, numGridCols];
            imgArray = new Image[numGridRows, numGridCols];
            Image temp_img;
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    rectArray[rr, cc] = new Rectangle() {
                        Stroke = System.Windows.Media.Brushes.Black,
                        // Height = (int)(gridHeight / numGridRows),
                        //Width = (int)(gridWidth / numGridRows),
                    };
                    temp_img = new Image();
                    BitmapImage bm = new BitmapImage();
                    bm.BeginInit();
                    bm.UriSource = new Uri("", UriKind.Relative);
                    bm.EndInit();
                    temp_img.Source = bm;
                    imgArray[rr, cc] = temp_img;
                    uniGrid.Children.Add(rectArray[rr, cc]);
                    uniImgGrid.Children.Add(imgArray[rr, cc]);
                }
            }

            //student storage initialization
            gridArray = new int[numGridRows, numGridCols];
            zeroGridArray();

            playerOneBrush = System.Windows.Media.Brushes.Green;

            playerTwoBrush = System.Windows.Media.Brushes.Red;

            isPlayerOneHandClosed = false; hasPlayerOneHandOpened = false;
            isPlayerTwoHandClosed = false;

            playerOneHandLocation = new Point();
            playerTwoHandLocation = new Point();

            gameOverDelay = 30;
            delayFrames = -1;
            currentRow = 1;
            isGameOver = false;
            waitingOnBot = false;
            countTime = 0;
            playerOneTrackingId = 0; playerTwoTrackingId = 0;
            needsAllocationPlayerOne = true; needsAllocationPlayerTwo = true;
            //resetGame();
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    //every frame we try to allocate a skeleton to a user -- if they are NOT found set them after this loop occurs
                    needsAllocationPlayerOne = true;
                    needsAllocationPlayerTwo = true;

                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;

                    if (needsAllocationPlayerOne && needsAllocationPlayerTwo)
                    {
                        //this state should really only be reached if the game has just started
                    }

                    //check if skeletons from last time are found -- if NOT allocate skeletons here
                    foreach (Body body in this.bodies)
                    {
                        if (body.IsTracked)
                        {
                            if (playerOneTrackingId == 0) // this has never been allocated
                            {
                                playerOneTrackingId = body.TrackingId;
                                break;
                            }

                            if (playerTwoTrackingId == 0) // this has never been allocated
                            {
                                playerTwoTrackingId = body.TrackingId;
                                break;
                            }
                        }
                    }

                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            // Console.WriteLine(body.TrackingId);                                                   

                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);

                            // A state change from Open -> Closed for a given hand is required for a closed hand to trigger
                            // the intention behind this is for code based on a closed hand only triggers ONCE
                            // otherwise code that only checks if a hand s closed will trigger every frame until it's open

                            //body.HandLeftState

                            HandState givenHandState;

                            if (useLeftHand)
                            {
                                givenHandState = body.HandLeftState;
                            }
                            else
                            {
                                givenHandState = body.HandRightState;
                            }

                            // Find the left hand state
                            switch (givenHandState)
                            {
                                case HandState.Open:
                                    //isPlayerOneHandClosed = false;
                                    hasPlayerOneHandOpened = true;
                                    isPlayerOneHandClosed = false;
                                    break;
                                case HandState.Closed:
                                    if (hasPlayerOneHandOpened)
                                    {
                                        isPlayerOneHandClosed = true;
                                        hasPlayerOneHandOpened = false;
                                        break;
                                    }

                                    isPlayerOneHandClosed = false;
                                    break;
                                default:
                                    isPlayerOneHandClosed = false;
                                    break;
                            }
                            //update location on board

                            //playerOneHandLocation
                            //playerTwoHandLocation


                            double shoulderLengthScale = Math.Sqrt(Math.Pow(jointPoints[JointType.ShoulderLeft].X - jointPoints[JointType.ShoulderRight].X, 2)
                                + Math.Pow(jointPoints[JointType.ShoulderLeft].Y - jointPoints[JointType.ShoulderRight].Y, 2));

                            //region is intended to scale hand values from 0-1, we have this region to the LEFT of the user
                            // which is scaled to the length of the left shoulder - to - right shoulder 

                            Rect handRegion;

                            if (useLeftHand) {
                                handRegion = new Rect(jointPoints[JointType.ShoulderLeft].X - (1.3 * shoulderLengthScale),
                                jointPoints[JointType.ShoulderLeft].Y - (1.0 * shoulderLengthScale),
                                2 * shoulderLengthScale,
                                2 * shoulderLengthScale);
                                playerOneHandLocation.X = (jointPoints[JointType.HandLeft].X - handRegion.X) / (handRegion.Width) * 600;
                                playerOneHandLocation.Y = (jointPoints[JointType.HandLeft].Y - handRegion.Y) / (handRegion.Height) * 600;
                            } else {
                                //using right hand
                                handRegion = new Rect(jointPoints[JointType.ShoulderRight].X - (.7 * shoulderLengthScale),
                                jointPoints[JointType.ShoulderRight].Y - (1.0 * shoulderLengthScale),
                                2 * shoulderLengthScale,
                                2 * shoulderLengthScale);
                                playerOneHandLocation.X = (jointPoints[JointType.HandRight].X - handRegion.X) / (handRegion.Width) * 600;
                                playerOneHandLocation.Y = (jointPoints[JointType.HandRight].Y - handRegion.Y) / (handRegion.Height) * 600;
                            }

                            dc.DrawRectangle(null, drawPen, handRegion);

                            Canvas.SetLeft(this.Hand, playerOneHandLocation.X - 25); // -25 to center hand on location
                            Canvas.SetTop(this.Hand, playerOneHandLocation.Y - 25); // -25 to center hand on location

                            studentWork();
                        }
                    }

                    //JointType.Hand
                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        ///////////////////////////
        // Functions for Students//
        ///////////////////////////

        // render an image at a given grid location
        // need to specify image name in the Images/ folder.
        // note: you must right click on the Images/ folder in the solution explorer
        //      and then Add>ExistingItem. Click on your .png file (which is already in the Images/ directory)
        //      and this will add it to the project. You must restart Visual Studio for the new item to be
        //      detected and used by our program.
        // to reset the image, simply specify "" as a filename
        // usage: draw_image(0,0,"saturn.png")
        private void draw_image(int row, int col, String filename)
        {

            BitmapImage bm = new BitmapImage();
            bm.BeginInit();
            bm.UriSource = new Uri("..\\..\\..\\Images\\" + filename, UriKind.Relative);
            bm.EndInit();

            imgArray[row, col].Source = bm;
            InvalidateVisual();
        }

        // reset all grid square colors to white
        void clearBoard()
        {
            BitmapImage bm = new BitmapImage();
            bm.BeginInit();
            bm.UriSource = new Uri("", UriKind.Relative);
            bm.EndInit();
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    rectArray[rr, cc].Fill = System.Windows.Media.Brushes.White;
                    imgArray[rr, cc].Source = bm;
                }
            }
        }

        // specify a location (row, col) to clear the color and reset to white.
        void clearGridLocation(int row, int col)
        {
            if (row < 0 || col < 0) return;
            rectArray[row, col].Fill = System.Windows.Media.Brushes.White;
        }

        // set the status box below the grid to a given string with a given color.
        // brush color examples:
        // System.Windows.Media.Brushes.Black
        // System.Windows.Media.Brushes.White
        // System.Windows.Media.Brushes.Green
        // System.Windows.Media.Brushes.Red
        void setText(string text, System.Windows.Media.Brush brush) { textHere.Text = text; textHere.Foreground = brush; }

        // update the instructions to the right side of the grid to a given string, color, and size.
        void setInstructionText(string text, System.Windows.Media.Brush Brush, double fontSize)
        {
            instructionText.Text = text;
            instructionText.Foreground = Brush;
            instructionText.FontSize = 36;
        }

        // update the text of a button given by a number (1,2,or 3)
        void setButtonText(string text, int buttonNum)
        {
            switch (buttonNum)
            {
                case 1:
                    Button1.Content = text;
                    break;
                case 2:
                    Button2.Content = text;
                    break;
                case 3:
                    Button3.Content = text;
                    break;
            }
        }

        // update color of a grid square given by row and column.
        void highlightGridLocationWithColor(int row, int col, System.Windows.Media.Brush brush)
        {
            if (row < 0 || col < 0) return;
            rectArray[row, col].Fill = brush;
            InvalidateVisual(); // render layout again
        }

        // update color of a grid square with a player's color
        // grid square is given by row and column
        void highlightGridLocation(int row, int col, int playerNumber)
        {
            if (row < 0 || col < 0) return;
            if (playerNumber == 1)
                rectArray[row, col].Fill = playerOneBrush;
            if (playerNumber == 2)
                rectArray[row, col].Fill = playerTwoBrush;
            InvalidateVisual(); // render layout again
        }

        // reset Grid Array to zero
        // this function is called upon for grid initialization
        void zeroGridArray()
        {
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    gridArray[rr, cc] = 0;
                }
            }
        }

        // sets a player’s color to a given brush color (accepted player numbers are 1 and 2).
        void setPlayerColor(int playerNumber, System.Windows.Media.Brush brush)
        {
            if (playerNumber == 1)
                playerOneBrush = brush;
            if (playerNumber == 2)
                playerTwoBrush = brush;
        }

        // If true, the given’s player hand is detected as closed
        // if false, it is detected as not closed (open).
        bool isPlayerHandClosed(int playerNumber)
        {
            if (playerNumber == 1)
                return isPlayerOneHandClosed;
            if (playerNumber == 2)
                return isPlayerTwoHandClosed;
            return false;
        }

        // returns a normalized point (0 to 1) of either player one hand's location or player two's
        // cannot return null point, so use with care
        // Note: values can be returned outside this range if the hand has left the region!
        Point getPlayerHandLocation(int playerNumber)
        {
            if (playerNumber == 1)
            {
                return new Point(playerOneHandLocation.X / gridWidth, playerOneHandLocation.Y / gridHeight);
            } else {
                return new Point(playerTwoHandLocation.X / gridWidth, playerTwoHandLocation.Y / gridHeight);
            }
        }

        // if the player’s hand is at the specified location, the function returns true, otherwise, false. 
        // note: this function doesn't tell WHICH location the playerHand is in
        // so it is necessary for the players to check EACH location and perform logic
        bool isPlayerHandInGridLocation(int row, int col, int playerNumber)
        {
            if (row < 0 || col < 0) return false;

            //resolution of drawing region is 600x600 -- we segment each region into parts
            //gridHeight, gridWidth

            double xLowerBound = col * (gridWidth / numGridCols), xUpperBound = (col + 1) * (gridWidth / numGridCols),
                yLowerBound = row * (gridHeight / numGridRows), yUpperBound = (row + 1) * (gridHeight / numGridRows);

            Point HandLocation;

            if (playerNumber == 1) HandLocation = playerOneHandLocation;
            else HandLocation = playerTwoHandLocation;

            if (HandLocation.X >= xLowerBound && HandLocation.X < xUpperBound &&
                HandLocation.Y >= yLowerBound && HandLocation.Y < yUpperBound)
            {
                return true;
            }

            return false;
        }

        /////////////////////////////////////
        // Functions for Students to Change//
        /////////////////////////////////////


        // used in tic tac toe and returns 0 no winner, returns 1 or 2 if that player has won respectively
        // this only works for a 3x3 game of tic tac toe.

        //assuming gridArray is 3 x 3


        //check diagonals
        /*  if (gridArray[0, 0] != 0 && gridArray[0, 0] == gridArray[1, 1] && gridArray[1, 1] == gridArray[2, 2])
          {
              return gridArray[0, 0];
          }

          if (gridArray[2, 0] != 0 && gridArray[2, 0] == gridArray[1, 1] && gridArray[1, 1] == gridArray[0, 2])
          {
              return gridArray[2, 0];
          }

          //check rows
          if (gridArray[0, 0] != 0 && gridArray[0, 0] == gridArray[0, 1] && gridArray[0, 1] == gridArray[0, 2])
          {
              return gridArray[0, 0];
          }

          if (gridArray[1, 0] != 0 && gridArray[1, 0] == gridArray[1, 1] && gridArray[1, 1] == gridArray[1, 2])
          {
                 return gridArray[1, 0];
          }

          if (gridArray[2, 0] != 0 && gridArray[2, 0] == gridArray[2, 1] && gridArray[2, 1] == gridArray[2, 2])
          {
              return gridArray[2, 0];
          }

          // check columns
          // check rows
          if (gridArray[0, 0] != 0 && gridArray[0, 0] == gridArray[1, 0] && gridArray[1, 0] == gridArray[2, 0])
          {
              return gridArray[0, 0];
          }

          if (gridArray[0, 1] != 0 && gridArray[0, 1] == gridArray[1, 1] && gridArray[1, 1] == gridArray[2, 1])
          {
              return gridArray[0, 1];
          }

          if (gridArray[0, 2] != 0 && gridArray[0, 2] == gridArray[1, 2] && gridArray[1, 2] == gridArray[2, 2])
          {
              return gridArray[0, 2];
          }

          return 0;
      }
      */

        void  kill()
        {
            bool noHit;
            bool foundInvader;
            int kk;

            foundInvader = false;
            kk = numGridRows - 1;

            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    if (isPlayerHandInGridLocation(rr, cc, 1) && isPlayerHandClosed(1))
                    {
                        kk = numGridRows - 1;
                        while (!foundInvader && kk >= 0)
                        {
                            if (gridArray[kk, cc] == 1)
                            {
                                gridArray[kk, cc] = 0;
                                foundInvader = true;
                                Console.WriteLine("FOUND INVADER!!");
                            }
                            kk--;
                        }
                    }
                }
            }
        }

        void moveDown()
        {
            // move every row one level down
            for (int rr = numGridRows - 2; rr > -1; rr--)
            {
                for (int cc = numGridCols - 1; cc > -1; cc--)
                {
                    /*if (gridArray[rr - 1, cc] == 1 && rr != 0)
                    {
                        gridArray[rr, cc] = 1;
                        gridArray[rr - 1, cc] = 0;
                    } */

                    if (rr != numGridRows - 1)
                    {
                        gridArray[rr + 1, cc] = gridArray[rr, cc];
                        gridArray[rr, cc] = 0;
                    }
                }
            }

            for (int cc = 0; cc < numGridCols; cc++)
                {
                    if (gridArray[numGridRows - 1, cc] == 1)
                    {
                        gridArray[numGridRows - 1, cc] = 2;
                    }
                }


        }





        // clears and resets the game state
        void resetGame()
        {
            // reset game
            waitingOnBot = false;
            isGameOver = false;
            clearBoard();
            zeroGridArray();
            /*  for (int rr = 0; rr < numGridRows; rr++)
              {
                  for (int cc = 0; cc < numGridCols; cc++)
                  {
                      if (rr == 0)
                      {
                          gridArray[rr,cc] = 1;
                      }
                      else
                      {
                          gridArray[rr,cc] = 0;
                      }
                  }
              }*/
        }


        // our simple computer AI plays by filling in squares from top to bottom
        /*void botMove()
        {
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    //fills in first empty spot in grid
                    if (gridArray[rr, cc] == 0)
                    {
                        gridArray[rr, cc] = 2;
                        highlightGridLocation(rr, cc, 2);
                        return;
                    }
                }
            }
            // if a bot has reached here the game is a TIE
            isGameOver = true;
            setText("TIE Game! (Close Hand to Reset)", System.Windows.Media.Brushes.Black);
        }*/

        void paintScreen()
        {
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    if (gridArray[rr, cc] == 0)
                    {
                        highlightGridLocationWithColor(rr, cc, System.Windows.Media.Brushes.White);
                    }
                    else if (gridArray[rr, cc] == 1)
                    {
                        highlightGridLocationWithColor(rr, cc, System.Windows.Media.Brushes.Green);
                    }
                    else if (gridArray[rr, cc] == 2)
                    {
                        highlightGridLocationWithColor(rr, cc, System.Windows.Media.Brushes.Red);
                    }
                }
            }
        }
        // Student generated code should be in this location. Currently, the game is tic tac toe.
        void studentWork()
        {
            // Reminder: Boolean Value is either true or false, "playerMoved" will be used later on in the code
            bool playerMoved = false;

            if (isGameOver == true && isPlayerHandClosed(1))
            {
                resetGame();
            }
           
            if (isGameOver == true && gameOverDelay == 0)
            {
                clearBoard();
                return;
            }


            kill();
            clearBoard();
            paintScreen();

            // rowHit and colHit are variables that are set to whatever row/col the player closes their hand in (You will see later on)
            int rowHit = -1, colHit = -1;

            /*for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {

                    Console.Write(gridArray[rr, cc] + " ");

                }
                Console.WriteLine();
            }

            Console.WriteLine();
            */

            // set instruction text -- naive set text, gets set EVERY function call
            // Newline => \r\n (Windows Format)
           setInstructionText(" Shoot the oncoming aliens \r\n before they reach the bottom! \r\n\r\n Move your left hand \r\n to navigate. \r\n\r\n Close your hand to shoot. \r\n\r\n Score = "+countTime, System.Windows.Media.Brushes.Black, 36);
    
            // set button text

            setButtonText("New Game", 1);

            setText("Invasion!", System.Windows.Media.Brushes.Black);

            // We use the gridArray as follows: 0 means the spot is BLANK, 1 means OCCUPIED by player 1, 2 means OCCUPIED by player 2
            // for the purposes of this bot (Computer AI), the user is always player 1

            // BOT MOVE

            // delayFrames can be set to a certain number in order to "stall" the program
            // REMEMBER: Camera operates at 30 frames per second. That means, if delayFrames == 90 then the program waits 3 seconds until it moves to the "else if" statement
            if (delayFrames > 0)
            {
                // Post-decrement delayFrames
                delayFrames--;

                // Return stops all execution of the StudentWork function
                // In the scope of this Kinect program, that means
                // that the program will then capture another frame and then start from the beginning of this function again. 
                // BE CAREFUL WHEN YOU USE RETURNS
                return;
            }
            else if (delayFrames == 0)
            {
                // delay is over
                // Put what you want to execute AFTER the delay WITHIN this section


                clearBoard();
                Console.WriteLine("HERE!");
                moveDown();
                paintScreen();

                for (int rr = 0; rr < numGridRows; rr++)
                {
                    for (int cc = 0; cc < numGridCols; cc++)
                    {
                        Console.Write("" + gridArray[rr, cc]);
                    }
                    Console.WriteLine("\n");
                }

               


                delayFrames--;

                countTime++;
                Console.WriteLine(countTime);
            }
            //currentRow = currentRow + 1;

            // Function that has the Bot make a move
            // You can edit the botMove function to make it more smarterer
            /*botMove(); 
            */
            // Checks if game is finished (ie. 3 in a row)
            //if (isGameOver == true)
            // Exit the function, this stops the program from continuing to check for winners because obviously the winner has already been found
            //{
            //    clearBoard();
            //}
            /*
            // checkWinner() returns 1 or 2 if there is a winner or 0 if there is no winner. 
            if (checkWinner() == 2) //Winner is player 2
            { 
                setText("BOT Wins! (Close Hand to Reset)", System.Windows.Media.Brushes.Red); //Changes the text on the screen
                isGameOver = true; // Changes this global variable so that the game ends
                return; // Exits the function
            }
            */
            // Bot has moved, it's the players' turn
            //waitingOnBot = false;*/

            // Exit the function
            //return;
            else
            {
                delayFrames = 50;
            }


            // GAME RESET LOGIC









            // Basic check victory
            /* if (isGameOver && isPlayerHandClosed(1)) // Resets game if your hand is closed
             {
                 resetGame();
                 setText("Space Invaders (use left hand)", System.Windows.Media.Brushes.Black);
                 return;
             }
             */

            // PLAYER MOVE 

            // Check if there was a move somewhere (hand hovers over button AND hand is closed)

            /*for (int rr = 0; rr < numGridRows; rr++) //Indexes through the rows
            {
            
            int rr = 2;
            for (int cc = 0; cc < numGridCols; cc++) //For each row, indexes through the column (Nested FOR loop)
            {
                // if the triggered location is already filled ignore it
                if (gridArray[rr, cc] == 0 && isPlayerHandInGridLocation(rr, cc, 1) &&
                    isPlayerHandClosed(1)) //Hand is closed within the given row and column
                {
                    rowHit = rr; // Record row
                    colHit = cc; // Record column
                    playerMoved = true;
                    gridArray[0, 0] = 1;
                    gridArray[0, 1] = 1;
                    gridArray[0, 2] = 1;
                    clearGridLocation(0, 0);
                    clearGridLocation(0, 1);
                    clearGridLocation(0, 2);
                    highlightGridLocation(1, 0, 1);
                    highlightGridLocation(1, 1, 1);
                    highlightGridLocation(1, 2, 1);
                    setText("P1 Wins! (Close Hand to Reset)", System.Windows.Media.Brushes.Green);
                    isGameOver = true;
                }
            }
            */

            Random x = new Random();
            x.Next(0, numGridCols);

            gridArray[0, x.Next(0, numGridCols)] = 1;
            gridArray[0, x.Next(0, numGridCols)] = 1;
            gridArray[0, x.Next(0, numGridCols)] = 1;
                
            

            for (int cc = 0; cc < numGridCols; cc++)
            {
                if (gridArray[numGridRows - 1, cc] == 2)
                {
                    isGameOver = true;
                    gameOverDelay--;
                }
            }




            // PLAYER MOVED LOGIC

            /*if (playerMoved && !waitingOnBot) 
            {
                // process PlayerOne move            
                gridArray[rowHit, colHit] = 1; // Internal grid (or "Board) is set to 1 at player hand location
                highlightGridLocation(rowHit, colHit, 1); 

                // Same logic as when checking if the Bot won
                if (checkWinner() == 1) 
                { 
                    setText("P1 Wins! (Close Hand to Reset)", System.Windows.Media.Brushes.Green); 
                    isGameOver = true; 
                    return; 
                }
                else
                {
                    // Sets delay to 15 which means the Bot will not make a move until 15 frames or 0.5 seconds
                    delayFrames = 15; //wait 15 event calls
                    waitingOnBot = true;
                }

            }
            */

        }

        // Button Logic
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            resetGame();
            setText("2", System.Windows.Media.Brushes.Black);
            return;
        }


        // optional: add functionality
        private void Button2_Click(object sender, RoutedEventArgs e)
        {

        }

        // optional: add functionality
        private void Button3_Click(object sender, RoutedEventArgs e)
        {

        }

    } // end bracket of studentWork()
} // end bracket of MainWindow() 
