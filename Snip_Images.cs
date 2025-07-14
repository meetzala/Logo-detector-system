﻿using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogoRecognitionSystem
{
    public partial class Snip_Images : Form
    {

        #region class info 

        Bitmap orginal_Image { get; set; }

        List<Bitmap> SelectedImages = new List<Bitmap>();

        List<string> IdealSets = new List<string>();
        int CurrentImageIndex { get; set; }

        string AvilebleIdealSets = string.Empty;

        Rectangle rect;

        Point StartLocation;

        Point EndLocation;

        bool IsMouseDown = false;

        #endregion

        #region Constructor
        public Snip_Images()
        {
            InitializeComponent();
            Central_Static_Value.snipImage = this;
            refrech_ideal_set_comboBox();

        }
        #endregion

        #region Events

    
        private void Extract_Blobs_bgw_DoWork(object sender, DoWorkEventArgs e)
        {

            if (orginal_Image != null)
            {


                SafeInvoke(waiting_for_extract_blobs, new Action(() => { this.waiting_for_extract_blobs.Visible = true; }));

                List<Rectangle> selectedImages = ImageHelper.Selectivesearch(orginal_Image, Central_Static_Value.Settings.base_k_, Central_Static_Value.Settings.Increment_k, Central_Static_Value.Settings.Img_Minimal_Width, Central_Static_Value.Settings.Img_Minimal_Height, Central_Static_Value.Settings.Img_Maximal_Width, Central_Static_Value.Settings.Img_Maximal_Height);

                

                this.SelectedImages = selectedImages.Select(x =>
                {
                  Bitmap img = ImageHelper.CutImage(x, this.orginal_Image);
                    switch (Central_Static_Value.Settings.Filter)
                    {

                        case "Colored": img = ImageHelper.BGRFiler(img); break;
                        case "Gray": img = ImageHelper.GrayFilter(img); break;
                        case "BlackWeight": img = ImageHelper.BlackWhiteFilter(img); break;
                        default: img = ImageHelper.BGRFiler(img); break;


                    }

                    return img;
                }
                ).ToList();

                try
                {
                    foreach (Rectangle rectangle in selectedImages)
                    {

                        using (var graphics = Graphics.FromImage(orginal_Image))
                        {
                            graphics.DrawRectangle(Pens.Black, rectangle);
                        }
                    }

                    SafeInvoke(docu_pb, new Action(() => {
                        docu_pb.Image = orginal_Image;
                    }));


                    CurrentImageIndex = selectedImages.Count - 1;
                    SafeInvoke(sniped_img_pictureBox, new Action(() => {
                        sniped_img_pictureBox.Image = ImageHelper.CutImage(selectedImages[CurrentImageIndex], orginal_Image);
                    }));
                    SafeInvoke(current_selected_image, new Action(() => {
                        this.current_selected_image.Text = (selectedImages.Count - CurrentImageIndex).ToString() + @"\" + (selectedImages.Count).ToString();
                    }));

                    SafeInvoke(waiting_for_extract_blobs, new Action(() => { this.waiting_for_extract_blobs.Visible = false; }));

                }
                catch (Exception ex)
                {

                    SafeInvoke(this, new Action(() => {
                        MessageBox.Show(ex.Message);
                    }));
                    SafeInvoke(waiting_for_extract_blobs, new Action(() => { this.waiting_for_extract_blobs.Visible = false; }));


                }


            }
        }
        private void Open_image_button_Click(object sender, EventArgs e)
        {
            OpenFileDialog getImag = new OpenFileDialog();

            getImag.Filter = "JPG,PNG|*.png;*.jpg";

            DialogResult result = getImag.ShowDialog();

            string Source_Logo_Link = string.Empty;


            if (result == DialogResult.OK)
            {

                docu_pb.reset();

                Bitmap image = new Bitmap(Image.FromFile(getImag.FileName));

                orginal_Image = ImageHelper.ResizeImage(docu_pb.Width, docu_pb.Height, image);

                docu_pb.NewImage=orginal_Image;

            }
        }

      

        private void Extract_Blobs_button_Click(object sender, EventArgs e)
        {
            reset();
            if (!Extract_Blobs_bgw.IsBusy) { 
                Extract_Blobs_bgw.RunWorkerAsync();
            }
        }

        private void Image_pictureBox_MouseDown(object sender, MouseEventArgs e)
        {

            IsMouseDown = true;

            StartLocation = e.Location;


        }

        private void Image_pictureBox_MouseMove(object sender, MouseEventArgs e)
        {

            posion_label.Text = "x : " + e.X.ToString() + ", " + "y : " + e.Y.ToString();

            if (IsMouseDown == true)
            {

                EndLocation = e.Location;
                docu_pb.ImagePB.Invalidate();

            }

        }

        private void Image_pictureBox_Paint(object sender, PaintEventArgs e)
        {

            if (rect != null && docu_pb.Image != null)
            {

                e.Graphics.DrawRectangle(Pens.Black, GetRectangle());

            }

        }


        private void Image_pictureBox_MouseUp(object sender, MouseEventArgs e)
        {

            if (IsMouseDown == true && docu_pb.Image != null)
            {

                EndLocation = e.Location;

                IsMouseDown = false;

                if (rect != null)
                {

                    if (rect.Width > 1 && rect.Height > 1 && this.docu_pb.Image != null)
                    {




                        DialogResult res = CustomMsgbox.InputBox.ShowDialog("represent the  Image a new Dataset  ?", "Question",
                                                     CustomMsgbox.InputBox.Icon.Question,
                                                     CustomMsgbox.InputBox.Buttons.YesNo);
                        if (res == System.Windows.Forms.DialogResult.Yes)
                        {
                            res = CustomMsgbox.InputBox.ShowDialog("Please Enter the Ideal set value !!", "Ideal set",
                           CustomMsgbox.InputBox.Icon.Question,
                          CustomMsgbox.InputBox.Buttons.OkCancel,
                          CustomMsgbox.InputBox.Type.TextBox,
                          new string[] { "Ideal set new Value" });

                            if (res == System.Windows.Forms.DialogResult.OK)
                            {

                                Bitmap image = ImageHelper.CutImage(rect, new Bitmap(this.docu_pb.Image));

                                switch (Central_Static_Value.Settings.Filter)
                                {

                                    case "Colored": image = ImageHelper.BGRFiler(image); break;
                                    case "Gray": image = ImageHelper.GrayFilter(image); break;
                                    case "BlackWeight": image = ImageHelper.BlackWhiteFilter(image); break;
                                    default: image = ImageHelper.BGRFiler(image); break;


                                }
                                string InputBox_ResultValue = CustomMsgbox.InputBox.ResultValue;

                                int match = this.AssignIdentity(InputBox_ResultValue);

                                string imageName = "image" + Central_Static_Value.Train_Model.to_Train_Images_dataGridView.Rows.Count.ToString();

                                Central_Static_Value.Train_Model.to_Train_Images_dataGridView.Rows.Add(imageName, InputBox_ResultValue, ImageHelper.ResizeImage(32, 32, ImageHelper.ResizeImage(32, 32, image)));

                                TrainingSet trainingSet = new TrainingSet(imageName, match, ImageHelper.ResizeImage(32, 32, image), true);

                                Central_Static_Value.Train_Model.TrainingSetList.Add(trainingSet);

                                refrech_ideal_set_comboBox();

                            }

                        }
                        else
                        {

                            res = CustomMsgbox.InputBox.ShowDialog("Please choose the Ideal set value !!", "Ideal set",
                            CustomMsgbox.InputBox.Icon.Question,
                            CustomMsgbox.InputBox.Buttons.OkCancel,
                            CustomMsgbox.InputBox.Type.ComboBox,
                            Central_Static_Value.Train_Model.neuron2identity.Select(x => x.Value).ToArray());

                            if (res == DialogResult.OK)
                            {
                                string result = CustomMsgbox.InputBox.ResultValue;

                                Bitmap image = ImageHelper.CutImage(rect, new Bitmap(this.docu_pb.Image));

                                string imageName = "image" + Central_Static_Value.Train_Model.to_Train_Images_dataGridView.Rows.Count.ToString();

                                int match = AssignIdentity(result);

                                switch (Central_Static_Value.Settings.Filter)
                                {

                                    case "Colored": image = ImageHelper.BGRFiler(image); break;
                                    case "Gray": image = ImageHelper.GrayFilter(image); break;
                                    case "BlackWeight": image = ImageHelper.BlackWhiteFilter(image); break;
                                    default: image = ImageHelper.BGRFiler(image); break;


                                }

                                Central_Static_Value.Train_Model.to_Train_Images_dataGridView.Rows.Add(imageName, result, ImageHelper.ResizeImage(32, 32, image));

                                TrainingSet trainingSet = new TrainingSet(imageName, match, ImageHelper.ResizeImage(32, 32, image), false);

                                Central_Static_Value.Train_Model.TrainingSetList.Add(trainingSet);

                                refrech_ideal_set_comboBox();


                            }
                        }

                    }

                }


            }

        }
         
        private void next_button_Click(object sender, EventArgs e)
        {
            if (SelectedImages.Any())
            {
                if (CurrentImageIndex != 0)
                {
                    CurrentImageIndex -= 1;
                    sniped_img_pictureBox.Image = SelectedImages[CurrentImageIndex];
                    this.current_selected_image.Text = (SelectedImages.Count - CurrentImageIndex).ToString() + @"\" + (SelectedImages.Count).ToString();


                }
                else
                {

                    CurrentImageIndex = SelectedImages.Count - 1;
                    sniped_img_pictureBox.Image = SelectedImages[CurrentImageIndex];
                    this.current_selected_image.Text = (SelectedImages.Count - CurrentImageIndex).ToString() + @"\" + (SelectedImages.Count).ToString();
                }

            }
        }

        private void back_button_Click(object sender, EventArgs e)
        {
            if (SelectedImages.Any())
            {
                if (CurrentImageIndex != (SelectedImages.Count - 1))
                {
                    CurrentImageIndex += 1;
                    sniped_img_pictureBox.Image = SelectedImages[CurrentImageIndex];
                    this.current_selected_image.Text = (SelectedImages.Count - CurrentImageIndex).ToString() + @"\" + (SelectedImages.Count).ToString();


                }
                else
                {

                    CurrentImageIndex = 0;
                    sniped_img_pictureBox.Image = SelectedImages[CurrentImageIndex];
                    this.current_selected_image.Text = (SelectedImages.Count - CurrentImageIndex).ToString() + @"\" + (SelectedImages.Count).ToString();
                }
            }
        }

        private void Export_button_Click(object sender, EventArgs e)
        {

            try
            {

                if (!string.IsNullOrEmpty(Image_name_tb.Text) && ideal_set_cb.SelectedIndex != -1 && sniped_img_pictureBox.Image != null)
                {

                    string imageName = Image_name_tb.Text;

                    string selecteditem = ideal_set_cb.SelectedItem.ToString();

                    int match = AssignIdentity(selecteditem);

                    Central_Static_Value.Train_Model.to_Train_Images_dataGridView.Rows.Add(imageName, selecteditem, ImageHelper.ResizeImage(32, 32, new Bitmap(sniped_img_pictureBox.Image)));

                    TrainingSet trainingSet = new TrainingSet(imageName, match, ImageHelper.ResizeImage(32, 32, new Bitmap(sniped_img_pictureBox.Image)), false);

                    Central_Static_Value.Train_Model.TrainingSetList.Add(trainingSet);

                }
                else
                {

                    MessageBox.Show(this, "please fill all requiret Felds", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    

                }

                MessageBox.Show(this, "Successfully Exported Data Set", "INfO",MessageBoxButtons.OK,MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {

                MessageBox.Show(this,ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        #endregion

        #region Methods

        void reset()
        {
            this.docu_pb.refrech_image();
            this.orginal_Image = docu_pb.Image;
            this.current_selected_image.Text = @"0\0";
            this.SelectedImages.Clear();

        }
        Rectangle GetRectangle()
        {
            rect = new Rectangle();
            rect.X = Math.Min(StartLocation.X, EndLocation.X);
            rect.Y = Math.Min(StartLocation.Y, EndLocation.Y);
            rect.Width = Math.Abs(StartLocation.X - EndLocation.X);
            rect.Height = Math.Abs(StartLocation.Y - EndLocation.Y);

            return rect;
        }
        public void refrech_ideal_set_comboBox()
        {

            foreach (KeyValuePair<int, string> keyValue in Central_Static_Value.Train_Model.neuron2identity)
                if (!ideal_set_cb.Items.Contains(keyValue.Value))
                    ideal_set_cb.Items.Add(keyValue.Value);


        }

      
        int AssignIdentity(String identity)
        {
            if (Central_Static_Value.Train_Model.identity2neuron.ContainsKey(identity.ToLower()))
            {
                return Central_Static_Value.Train_Model.identity2neuron[identity.ToLower()];
            }

            int result = Central_Static_Value.Train_Model.outputCount;

            Central_Static_Value.Train_Model.identity2neuron[identity.ToLower()] = result;

            Central_Static_Value.Train_Model.neuron2identity[result] = identity.ToLower();

            Central_Static_Value.Train_Model.outputCount++;

            return result;

        }
        public void SafeInvoke(Control uiElement, Action updater)
        {

            if (uiElement == null)
            {
                throw new ArgumentNullException("Control was null");
            }

            if (uiElement.InvokeRequired)
            {


                uiElement.BeginInvoke((Action)delegate { SafeInvoke(uiElement, updater); });

            }
            else
            {
                if (uiElement.IsDisposed)
                {
                    throw new ObjectDisposedException("Control is already disposed.");
                }

                updater();
            }

        }








        #endregion
    }
}
