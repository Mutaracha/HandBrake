// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TimeMaskBehavior.cs" company="HandBrake Project (https://handbrake.fr)">
//   This file is part of the HandBrake source code - It may be used under the terms of the GNU General Public License.
// </copyright>
// <summary>
//   Provides fixed-width HH:MM:SS editing for a TextBox.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HandBrakeWPF.Behaviours
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using Microsoft.Xaml.Behaviors;

    /// <summary>
    /// Edits a six-digit time value without allowing the mask separators to be changed.
    /// </summary>
    public class TimeMaskBehavior : Behavior<TextBox>
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
            nameof(IsEnabled),
            typeof(bool),
            typeof(TimeMaskBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

        private static readonly int[] DigitCaretPositions = { 0, 1, 3, 4, 6, 7 };

        private readonly char[] digits = { '0', '0', '0', '0', '0', '0' };

        private bool updatingText;

        public bool IsEnabled
        {
            get => (bool)this.GetValue(IsEnabledProperty);
            set => this.SetValue(IsEnabledProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.PreviewTextInput += this.OnPreviewTextInput;
            this.AssociatedObject.PreviewKeyDown += this.OnPreviewKeyDown;
            this.AssociatedObject.TextChanged += this.OnTextChanged;
            DataObject.AddPastingHandler(this.AssociatedObject, this.OnPaste);

            this.ReadDigitsFromText();
        }

        protected override void OnDetaching()
        {
            DataObject.RemovePastingHandler(this.AssociatedObject, this.OnPaste);
            this.AssociatedObject.PreviewTextInput -= this.OnPreviewTextInput;
            this.AssociatedObject.PreviewKeyDown -= this.OnPreviewKeyDown;
            this.AssociatedObject.TextChanged -= this.OnTextChanged;

            base.OnDetaching();
        }

        private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            TimeMaskBehavior behavior = (TimeMaskBehavior)dependencyObject;
            if (behavior.AssociatedObject != null && behavior.IsEnabled)
            {
                behavior.ReadDigitsFromText();
                behavior.WriteDigits(0);
            }
        }

        private static int GetDigitIndex(int caretIndex)
        {
            if (caretIndex <= 2)
            {
                return caretIndex;
            }

            if (caretIndex <= 5)
            {
                return caretIndex - 1;
            }

            return Math.Min(caretIndex - 2, 6);
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!this.IsEnabled)
            {
                return;
            }

            e.Handled = true;
            this.InsertDigits(e.Text);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!this.IsEnabled || (e.Key != Key.Back && e.Key != Key.Delete))
            {
                return;
            }

            e.Handled = true;

            if (this.AssociatedObject.SelectionLength > 0)
            {
                this.ClearSelection();
                return;
            }

            int digitIndex = GetDigitIndex(this.AssociatedObject.CaretIndex);
            if (e.Key == Key.Back)
            {
                digitIndex--;
            }

            if (digitIndex >= 0 && digitIndex < this.digits.Length)
            {
                this.digits[digitIndex] = '0';
                this.WriteDigits(digitIndex);
            }
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!this.IsEnabled)
            {
                return;
            }

            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string text = e.DataObject.GetData(DataFormats.Text) as string;
                e.CancelCommand();
                this.InsertDigits(text);
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.IsEnabled && !this.updatingText)
            {
                this.ReadDigitsFromText();
            }
        }

        private void InsertDigits(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Any(character => character < '0' || character > '9'))
            {
                return;
            }

            int digitIndex = GetDigitIndex(this.AssociatedObject.SelectionStart);
            if (this.AssociatedObject.SelectionLength == this.AssociatedObject.Text.Length)
            {
                Array.Clear(this.digits, 0, this.digits.Length);
                digitIndex = 0;
            }

            foreach (char digit in text)
            {
                if (digitIndex >= this.digits.Length || !this.IsDigitValid(digitIndex, digit))
                {
                    break;
                }

                this.digits[digitIndex] = digit;
                digitIndex++;
            }

            this.WriteDigits(Math.Min(digitIndex, this.digits.Length));
        }

        private bool IsDigitValid(int digitIndex, char digit)
        {
            // The tens digits of minutes and seconds cannot exceed 5.
            return (digitIndex != 2 && digitIndex != 4) || digit <= '5';
        }

        private void ClearSelection()
        {
            int start = GetDigitIndex(this.AssociatedObject.SelectionStart);
            int end = GetDigitIndex(this.AssociatedObject.SelectionStart + this.AssociatedObject.SelectionLength);
            for (int index = start; index < end && index < this.digits.Length; index++)
            {
                this.digits[index] = '0';
            }

            this.WriteDigits(start);
        }

        private void ReadDigitsFromText()
        {
            string text = this.AssociatedObject.Text;
            if (text.Length != 8 || text[2] != ':' || text[5] != ':')
            {
                return;
            }

            string newDigits = text.Remove(5, 1).Remove(2, 1);
            if (newDigits.All(character => character >= '0' && character <= '9'))
            {
                for (int index = 0; index < this.digits.Length; index++)
                {
                    this.digits[index] = newDigits[index];
                }
            }
        }

        private void WriteDigits(int digitIndex)
        {
            this.updatingText = true;
            this.AssociatedObject.Text = string.Format(
                "{0}{1}:{2}{3}:{4}{5}",
                this.digits[0],
                this.digits[1],
                this.digits[2],
                this.digits[3],
                this.digits[4],
                this.digits[5]);
            this.updatingText = false;

            int caretIndex = digitIndex >= this.digits.Length
                                 ? 8
                                 : DigitCaretPositions[Math.Max(0, digitIndex)];
            this.AssociatedObject.Select(caretIndex, 0);
        }
    }
}
