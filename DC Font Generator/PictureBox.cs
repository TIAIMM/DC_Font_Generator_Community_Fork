/* Title:
 * PictureBox with zoom and scroll functionallity
 * 
 * Author:
 * Alexander Kloep Apr. 2005
 * Alexander.Kloep@gmx.net
 * 
 * Reason:
 * In a past project i designed a GUI with a PictureBox control on it. Because of the low screen 
 * resolution i couldn´t make the GUI big enough to show the whole picture. So i decided to develop
 * my own scrollable picturebox with the special highlight of zooming functionallity.
 * 
 * The solution: 
 * When the mouse cursor enters the ctrl, the cursorstyle changes and you are able to zoom in or out 
 * with the mousewheel. The princip of the zooming effect is to raise or to lower the inner picturebox 
 * size by a fixed zooming factor. The scroolbars appear automatically when the inner picturebox
 * gets bigger than the ctrl.
 *  
 * Here it is...
 * 
 * Last modification: 06/04/2005
 */

#region Usings

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

#endregion

namespace PictureBoxCtrl
{
    public delegate void ClickEventHandler(object sender, EventArgs e);
	/// <summary>
	/// Summary for the PictureBox Ctrl
	/// </summary>
	public class PictureBox : System.Windows.Forms.UserControl
	{
		#region Members

		public System.Windows.Forms.PictureBox PicBox;
		private System.Windows.Forms.Panel OuterPanel;
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.ToolTip toolTip1;
		private string m_sPicName = "";

        private Point mouse=new Point(0,0);
		#endregion

		#region Constants

		private double ZOOMFACTOR = 1.25;	// = 25% smaller or larger
		private int MINMAX = 5;				// 5 times bigger or smaller than the ctrl

		#endregion

		#region Designer generated code

		private void InitializeComponent()
		{
			this.PicBox = new System.Windows.Forms.PictureBox();
			this.OuterPanel = new System.Windows.Forms.Panel();
			this.OuterPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// PicBox
			// 
			this.PicBox.Location = new System.Drawing.Point(0, 0);
			this.PicBox.Name = "PicBox";
			this.PicBox.Size = new System.Drawing.Size(150, 140);
			this.PicBox.TabIndex = 3;
			this.PicBox.TabStop = false;
			// 
			// OuterPanel
			// 
			this.OuterPanel.AutoScroll = true;
			this.OuterPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.OuterPanel.Controls.Add(this.PicBox);
			this.OuterPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.OuterPanel.Location = new System.Drawing.Point(0, 0);
			this.OuterPanel.Name = "OuterPanel";
			this.OuterPanel.Size = new System.Drawing.Size(210, 190);
			this.OuterPanel.TabIndex = 4;
			// 
			// PictureBox
			// 
			this.Controls.Add(this.OuterPanel);
			this.Name = "PictureBox";
			this.Size = new System.Drawing.Size(210, 190);
			this.OuterPanel.ResumeLayout(false);
			this.ResumeLayout(false);

            this.toolTip1 = new System.Windows.Forms.ToolTip(new System.ComponentModel.Container());
            
            

		}
		#endregion

		#region Constructors

		public PictureBox()
		{
			InitializeComponent ();
			InitCtrl ();	// my special settings for the ctrl
		}

		#endregion

        public string ToolTip
        {
            set
            {
                //this.toolTip1.ToolTipTitle = "Titled ToolTip";
                this.toolTip1.SetToolTip(this.PicBox, value);
            }
        }
        public Image SetImage
        {
            set
            {
                PicBox.SizeMode = PictureBoxSizeMode.Normal;
                OuterPanel.AutoScrollPosition = new Point(0, 0);
                
                PicBox.Location = new Point(0, 0);
                
                PicBox.Image = value;
                PicBox.Width = value.Width;
                PicBox.Height = value.Height;
            }
        }
        public Image ChangeImage
        {
            set
            {
                PicBox.Image = value;
            }
        }

        public Point Mouse
        {
            get { return this.mouse; }
        }
		#region Properties

		/// <summary>
		/// Property to select the picture which is displayed in the picturebox. If the 
		/// file doesn´t exist or we receive an exception, the picturebox displays 
		/// a red cross.
		/// </summary>
		/// <value>Complete filename of the picture, including path information</value>
		/// <remarks>Supported fileformat: *.gif, *.tif, *.jpg, *.bmp</remarks>
		[ Browsable ( false ) ]
		public string PictureFromFile
		{
			get { return m_sPicName; }
			set 
			{
				if ( null != value )
				{
					if ( System.IO.File.Exists ( value ) )
					{
						try
						{
                            SetImage = Image.FromFile(value);
							m_sPicName = value;
						}
						catch ( OutOfMemoryException ex )
						{
                            string err = ex.ToString();
							RedCross ();
						}
					}
					else
					{				
						RedCross ();
					}
				}
			}
		}

		/// <summary>
		/// Set the frametype of the picturbox
		/// </summary>
		[ Browsable ( false ) ]
		public BorderStyle Border
		{
			get { return OuterPanel.BorderStyle; }
			set { OuterPanel.BorderStyle = value; }
		}

		#endregion

		#region Other Methods

		/// <summary>
		/// Special settings for the picturebox ctrl
		/// </summary>
		private void InitCtrl ()
		{
			PicBox.SizeMode = PictureBoxSizeMode.StretchImage;
			PicBox.Location = new Point ( 0, 0 );
			OuterPanel.Dock = DockStyle.Fill;
			//OuterPanel.Cursor = System.Windows.Forms.Cursors.NoMove2D;
			OuterPanel.AutoScroll = true;
			OuterPanel.MouseEnter += new EventHandler(PicBox_MouseEnter);
            PicBox.MouseHover += new EventHandler(PicBox_MouseHover);
            //OuterPanel.MouseHover += new EventHandler(PicBox_MouseHover);
			PicBox.MouseEnter += new EventHandler(PicBox_MouseEnter);
			//OuterPanel.MouseWheel += new MouseEventHandler(PicBox_MouseWheel);
            PicBox.MouseClick += new MouseEventHandler(PicBox_MouseClick);
            PicBox.MouseMove += new MouseEventHandler(PicBox_MouseClick);
            PicBox.MouseLeave += new System.EventHandler(this.PicBox_MouseLeave);
            OuterPanel.Scroll += new System.Windows.Forms.ScrollEventHandler(PicBox_Scroll);
		}
		/// <summary>
		/// Create a simple red cross as a bitmap and display it in the picturebox
		/// </summary>
		private void RedCross ()
		{
			Bitmap bmp = new Bitmap ( OuterPanel.Width, OuterPanel.Height, System.Drawing.Imaging.PixelFormat.Format16bppRgb555 );
			Graphics gr;
			gr = Graphics.FromImage ( bmp );
			Pen pencil = new Pen ( Color.Red, 5 );
			gr.DrawLine ( pencil, 0, 0, OuterPanel.Width, OuterPanel.Height );
			gr.DrawLine ( pencil, 0, OuterPanel.Height, OuterPanel.Width, 0  );
			PicBox.Image = bmp;
			gr.Dispose ();
		}

		#endregion

		#region Zooming Methods

		/// <summary>
		/// Make the PictureBox dimensions larger to effect the Zoom.
		/// </summary>
		/// <remarks>Maximum 5 times bigger</remarks>
		private void ZoomIn() 
		{
			if ( ( PicBox.Width < ( MINMAX * OuterPanel.Width ) ) &&
				( PicBox.Height < ( MINMAX * OuterPanel.Height ) ) )
			{
				PicBox.Width = Convert.ToInt32 ( PicBox.Width * ZOOMFACTOR );
				PicBox.Height = Convert.ToInt32 ( PicBox.Height * ZOOMFACTOR );
				PicBox.SizeMode = PictureBoxSizeMode.StretchImage; 
			}
		}

		/// <summary>
		/// Make the PictureBox dimensions smaller to effect the Zoom.
		/// </summary>
		/// <remarks>Minimum 5 times smaller</remarks>
		private void ZoomOut() 
		{
			if ( ( PicBox.Width > ( OuterPanel.Width / MINMAX ) ) &&
				( PicBox.Height > ( OuterPanel.Height / MINMAX ) ) )
			{
				PicBox.SizeMode = PictureBoxSizeMode.StretchImage; 
				PicBox.Width = Convert.ToInt32 ( PicBox.Width / ZOOMFACTOR );
				PicBox.Height = Convert.ToInt32 ( PicBox.Height / ZOOMFACTOR );
			}		
		}

        public bool SizeNormal()
        {
            if (PicBox.SizeMode != PictureBoxSizeMode.Normal)
            {
                PicBox.Width = Convert.ToInt32(PicBox.Width);
                PicBox.Height = Convert.ToInt32(PicBox.Height);
                PicBox.SizeMode = PictureBoxSizeMode.Normal;
                return true;
            }
            return false;
        }
        
        public void SizeStretch()
        {
            PicBox.SizeMode = PictureBoxSizeMode.StretchImage; 
        }
		#endregion

		#region Mouse events
        private void PicBox_MouseLeave(object sender, EventArgs e)
        {
            this.OnMouseLeave(e);
        }
		/// <summary>
		/// We use the mousewheel to zoom the picture in or out
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void PicBox_MouseWheel(object sender, MouseEventArgs e)
		{
			if ( e.Delta < 0 )
			{
				ZoomIn ();
			}
            else if (e.Delta == 0)
            {
                SizeNormal();
            }
            else
            {
                ZoomOut();
            }
		}

        private void PicBox_MouseClick(object sender, MouseEventArgs e)
        {
            this.OnMouseClick(e); 
        }
        private void PicBox_Scroll(object sender, ScrollEventArgs e)
        {
            this.OnScroll(e);
        }
        /// <summary>
		/// Make sure that the PicBox have the focus, otherwise it doesn´t receive 
		/// mousewheel events !.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PicBox_MouseEnter(object sender, EventArgs e)
		{
			if ( PicBox.Focused == false )
			{
				PicBox.Focus ();
			}
		}

        private void PicBox_MouseHover(object sender, EventArgs e)
        {
            mouse = ((System.Windows.Forms.PictureBox)sender).PointToClient(MousePosition);

            this.OnMouseHover(e);
        }

		#endregion

		#region Disposing

		/// <summary>
		/// Die verwendeten Ressourcen bereinigen.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		#endregion
	}
}
