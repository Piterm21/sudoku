#define fast

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PiotrBachurSudoku
{
    public struct subPanel
    {
        public Panel panel;
        public List<TextBox> inputFields;
    };

    public enum invalidityType
    {
        none = 0,
        column = 1 << 0,
        line = 1 << 1,
        box = 1 << 2,
    }

    public struct valueCheckResult
    {
        public int type;
        public List<TextBox> offenders;
    }

    public class filledBox
    {
        public int textBoxIndex;
        public int value;
        public int nextIndexToTry;
        public int[] values;
        public int lineIndex;
        public int columnIndex;
        public int groupIndex;
    }

    public partial class Form1 : Form
    {
        subPanel[] subPanels;
        List<TextBox>[] lines;
        List<TextBox>[] columns;
        List<TextBox> allFields;
        String solution;
        bool checkValues = true;
        Random rnd = new Random();
        bool hasErrors = false;

        public Form1 ()
        {
            InitializeComponent();

            int screenHeight = SystemInformation.VirtualScreen.Height;
            int maxSize = screenHeight - 200;
            int spaceingSmall = 1;
            int spaceingLarge = 3;
            maxSize = maxSize / 9 * 9;
            int singleBoxSize = maxSize / 9;
            maxSize += 2 * spaceingLarge + 4 * spaceingSmall;

            this.ClientSize = new Size(maxSize, maxSize + menuStrip1.Size.Height);
            Panel backgroundMain = new Panel();
            backgroundMain.Size = new Size(maxSize, maxSize + menuStrip1.Size.Height);
            backgroundMain.Location = new Point(0, menuStrip1.Size.Height);
            backgroundMain.BackColor = Color.Black;
            this.Controls.Add(backgroundMain);
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            subPanels = new subPanel[9];
            lines = new List<TextBox>[9];
            columns = new List<TextBox>[9];
            allFields = new List<TextBox>();

            for (int i = 0; i < 9; i++) {
                lines[i] = new List<TextBox>();
                columns[i] = new List<TextBox>();
            }

            for (int y = 0; y < 3; y++) {
                for (int x = 0; x < 3; x++) {
                    int subBackgroundSize = singleBoxSize * 3 + spaceingSmall * 2;
                    Point subBackgroundOriginPoint = new Point(x * (subBackgroundSize + spaceingLarge), y * (subBackgroundSize + spaceingLarge));

                    Panel secondaryBackground = new Panel();
                    secondaryBackground.Size = new Size(subBackgroundSize, subBackgroundSize);
                    secondaryBackground.Location = subBackgroundOriginPoint;
                    secondaryBackground.BackColor = Color.LightGray;
                    subPanels[x + y * 3].panel = secondaryBackground;
                    subPanels[x + y * 3].inputFields = new List<TextBox>();
                    backgroundMain.Controls.Add(secondaryBackground);

                    for (int innerY = 0; innerY < 3; innerY++) {
                        for (int innerX = 0; innerX < 3; innerX++) {
                            TextBox inputField = new TextBox();
                            inputField.TabIndex = innerX + innerY * 3 + x * 9 + y * 27;
                            inputField.BorderStyle = BorderStyle.None;
                            inputField.TextAlign = HorizontalAlignment.Center;
                            inputField.MinimumSize = new Size(singleBoxSize, singleBoxSize);
                            inputField.Size = new Size(singleBoxSize, singleBoxSize);
                            inputField.Location = new Point(innerX * (singleBoxSize + spaceingSmall), innerY * (singleBoxSize + spaceingSmall));
                            inputField.MaxLength = 1;
                            inputField.KeyPress += this.checkCharInput;
                            inputField.TextChanged += this.checkValueOnInput;
                            inputField.Font = new Font("Microsoft Sans Serif", singleBoxSize - 15, FontStyle.Regular, GraphicsUnit.Pixel, ((byte)(238)));

                            int subPanelIndex = x + y * 3;
                            int lineIndex = innerY + y * 3;
                            int columnIndex = innerX + x * 3;
                            inputField.Tag = "" + subPanelIndex + lineIndex + columnIndex + "U";
                            subPanels[subPanelIndex].inputFields.Add(inputField);
                            lines[lineIndex].Add(inputField);
                            columns[columnIndex].Add(inputField);
                            allFields.Add(inputField);

                            secondaryBackground.Controls.Add(inputField);
                        }
                    }
                }
            }
        }

        public void checkCharInput (object sender, KeyPressEventArgs e)
        {
            if ((!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) || e.KeyChar == '0') {
                e.Handled = true;
            }
        }

        public int extractValue (TextBox field)
        {
            int result = 0;

            if (field.Text != "") {
                result = int.Parse(field.Text);
            }

            return result;
        }

        public valueCheckResult checkForErrors (List<TextBox> list, int errorValue, int value, valueCheckResult resultToModify, String tag)
        {
            foreach (TextBox textBox in list) {
                if (this.extractValue(textBox) == value && textBox.Tag.ToString() != tag) {
                    resultToModify.type |= errorValue;
                    resultToModify.offenders.Add(textBox);
                    break;
                }
            }

            return resultToModify;
        }

        public valueCheckResult getInputErrors (String tag, int value)
        {
            valueCheckResult result = new valueCheckResult();
            result.type = (int)invalidityType.none;
            result.offenders = new List<TextBox>();

            int subGroupIndex = tag[0] - '0';
            int lineIndex = tag[1] - '0';
            int columnIndex = tag[2] - '0';

            result = this.checkForErrors(subPanels[subGroupIndex].inputFields, (int)invalidityType.box, value, result, tag);
            result = this.checkForErrors(lines[lineIndex], (int)invalidityType.line, value, result, tag);
            result = this.checkForErrors(columns[columnIndex], (int)invalidityType.column, value, result, tag);

            return result;
        }

        public void colorBoxes (List<TextBox> list, Color color, Color colorToSkip)
        {
            foreach (TextBox textBox in list) {
                if (textBox.BackColor != colorToSkip) {
                   textBox.BackColor = color;
                }
            }
        }

        public void handleErrorMarking (List<TextBox> list, int flagToCheck, valueCheckResult checkResult, Color color, Color colorToSkip)
        {
            if ((checkResult.type & flagToCheck) != 0) {
                this.colorBoxes(list, color, colorToSkip);
            }
        }

        public void clearColors ()
        {
            hasErrors = false;

            foreach (TextBox textBox in allFields) {
                if (textBox.Tag.ToString()[3] == 'U') {
                    textBox.BackColor = Color.White;
                } else {
                    textBox.BackColor = Color.LightBlue;
                }
            }
        }

        public void checkAllFieldsForErrors ()
        {
            bool fullyFilled = true;

            foreach (TextBox textBox in allFields) {
                int value = this.extractValue(textBox);

                if (value != 0) {
                    this.validateField(textBox, value);
                } else {
                    fullyFilled = false;
                }
            }

            if (fullyFilled && !hasErrors) {
                string message = "Congratulations! Play again?";
                string caption = "You won!";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result;
                result = MessageBox.Show(message, caption, buttons);

                if (result == System.Windows.Forms.DialogResult.Yes) {
                    this.clearFields();
                    this.generateSolvable();
                }
            }
        }

        public void validateField (TextBox inputField, int value)
        {
            valueCheckResult checkResult = this.getInputErrors(inputField.Tag.ToString(), value);

            int subGroupIndex = inputField.Tag.ToString()[0] - '0';
            int lineIndex = inputField.Tag.ToString()[1] - '0';
            int columnIndex = inputField.Tag.ToString()[2] - '0';

            if (checkResult.type != (int)invalidityType.none) {
                this.handleErrorMarking(subPanels[subGroupIndex].inputFields, (int)invalidityType.box, checkResult, Color.Red, Color.DarkRed);
                this.handleErrorMarking(lines[lineIndex], (int)invalidityType.line, checkResult, Color.Red, Color.DarkRed);
                this.handleErrorMarking(columns[columnIndex], (int)invalidityType.column, checkResult, Color.Red, Color.DarkRed);

                checkResult.offenders.Add(inputField);
                this.colorBoxes(checkResult.offenders, Color.DarkRed, Color.DarkOrchid);

                hasErrors = true;
            }
        }

        public void checkValueOnInput (object sender, EventArgs e)
        {
            if (checkValues) {
                TextBox inputField = (TextBox)sender;
                int value = this.extractValue(inputField);

                this.clearColors();
                this.checkAllFieldsForErrors();
            }
        }

        public T[] shuffle<T> (T[] array)
        {
            int i = array.Length;

            while (i > 1) {
                i--;

                int j = rnd.Next(0, i - 1);
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }

            return array;
        }

        public void clearField (TextBox textBox)
        {
            textBox.Text = "";
            String newTag = textBox.Tag.ToString();
            newTag = newTag.Replace('L', 'U');
            textBox.Enabled = true;
            textBox.BackColor = Color.White;
            textBox.Tag = newTag;
        }

        public void clearFields ()
        {
            foreach (TextBox textBox in allFields) {
                this.clearField(textBox);
            }
        }

        List<filledBox>[] generationLines;
        List<filledBox>[] generationColumns;
        List<filledBox>[] generationGroups;

        public filledBox[] shuffleBoxesCreateCheckingLists()
        {
            filledBox[] boxes = new filledBox[81];

            for (int i = 0; i < 81; i++) {
                boxes[i] = new filledBox();
                boxes[i].values = new int[9]{ 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                boxes[i].values = shuffle(boxes[i].values);
            }

            generationLines = new List<filledBox>[9];
            generationColumns = new List<filledBox>[9];
            generationGroups = new List<filledBox>[9];

            for (int i = 0; i < 9; i++) {
                generationLines[i] = new List<filledBox>();
                generationColumns[i] = new List<filledBox>();
                generationGroups[i] = new List<filledBox>();
            }

            int boxIndex = 0;
            for (int y = 0; y < 3; y++) {
                for (int x = 0; x < 3; x++) {
                    for (int innerY = 0; innerY < 3; innerY++) {
                        for (int innerX = 0; innerX < 3; innerX++) {
                            int subPanelIndex = x + y * 3;
                            int lineIndex = innerY + y * 3;
                            int columnIndex = innerX + x * 3;

                            boxes[boxIndex].textBoxIndex = boxIndex;
                            boxes[boxIndex].groupIndex = subPanelIndex;
                            boxes[boxIndex].columnIndex = columnIndex;
                            boxes[boxIndex].lineIndex = lineIndex;
                            boxes[boxIndex].nextIndexToTry = 0;
                            boxes[boxIndex].value = 0;

                            generationLines[lineIndex].Add(boxes[boxIndex]);
                            generationColumns[columnIndex].Add(boxes[boxIndex]);
                            generationGroups[subPanelIndex].Add(boxes[boxIndex]);
                            boxIndex++;
                        }
                    }
                }
            }

#if !fast
            boxes = shuffle(boxes);
#endif

            return boxes;
        }

        public bool hasError (List<filledBox> list, filledBox box)
        {
            bool result = false;

            foreach (filledBox filledBoxToCheck in list) {
                if (filledBoxToCheck.value == box.values[box.nextIndexToTry]) {
                    if (box.lineIndex != filledBoxToCheck.lineIndex ||
                        box.columnIndex != filledBoxToCheck.columnIndex) {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        public bool createsError (filledBox box)
        {
            bool result = false;

            if (hasError(generationGroups[box.groupIndex], box)) {
                result = true;
            } else if (hasError(generationLines[box.lineIndex], box)) {
                result = true;
            } else if (hasError(generationColumns[box.columnIndex], box)) {
                result = true;
            }

            return result;
        }

        public void generateSolvable()
        {
            checkValues = false;

            filledBox[] boxes = this.shuffleBoxesCreateCheckingLists();

            long iter = 0;
            int nextIndexToFill = 0;
            while (nextIndexToFill != 81) {
                for (; boxes[nextIndexToFill].nextIndexToTry < 9; boxes[nextIndexToFill].nextIndexToTry++) {
                    // check if is valid
                    if (!createsError(boxes[nextIndexToFill])) {
                        boxes[nextIndexToFill].value = boxes[nextIndexToFill].values[boxes[nextIndexToFill].nextIndexToTry];
                        nextIndexToFill++;
                        break;
                    }

                    if (boxes[nextIndexToFill].nextIndexToTry == 8) {
                        boxes[nextIndexToFill].nextIndexToTry = 0;
                        boxes[nextIndexToFill].value = 0;
                        nextIndexToFill--;
                        boxes[nextIndexToFill].nextIndexToTry++;

                        while (boxes[nextIndexToFill].nextIndexToTry == 9) {
                            boxes[nextIndexToFill].nextIndexToTry = 0;
                            boxes[nextIndexToFill].value = 0;
                            nextIndexToFill--;
                            boxes[nextIndexToFill].nextIndexToTry++;
                        }

                        break;
                    }
                }

                iter++;
            }

            System.Diagnostics.Debug.WriteLine(iter);

            foreach (filledBox box in boxes) {
                allFields[box.textBoxIndex].Text = box.value.ToString();
                allFields[box.textBoxIndex].Tag = allFields[box.textBoxIndex].Tag.ToString().Replace('U', 'L');
                allFields[box.textBoxIndex].BackColor = Color.LightBlue;
                allFields[box.textBoxIndex].Enabled = false;
            }

            solution = "";
            foreach (TextBox box in allFields) {
                solution += box.Text;
            }

            int[][] groupRemovalOrder = new int[9][];
            for (int i = 0; i < 9; i++) {
                groupRemovalOrder[i] = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
                shuffle(groupRemovalOrder[i]);
            }

            for (int i = 0; i < 6; i++) {
                int groupIndex = 0;
                for (int j = 0; j < 9; j++) {
                    clearField(subPanels[groupIndex].inputFields[groupRemovalOrder[groupIndex][i]]);
                    groupIndex++;
                }
            }

            checkValues = true;
        }

        public void generatePuzzle (object sender, EventArgs e)
        {
            this.clearFields();
            this.generateSolvable();
        }

        private void saveToolStripMenuItem_Click (object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "GML|*.gml";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK) {
                if (saveFileDialog1.FileName != "") {
                    System.IO.FileStream fileStream = (System.IO.FileStream)saveFileDialog1.OpenFile();

                    String valuesString = "";

                    foreach (TextBox textBox in allFields) {
                        valuesString += "" + this.extractValue(textBox) + textBox.Tag.ToString()[3].ToString();
                    }

                    valuesString += "," + solution;

                    byte[] bytes = Encoding.ASCII.GetBytes(valuesString);
                    fileStream.Write(bytes, 0, bytes.Length);

                    fileStream.Close();
                }
            }
        }

        private void loadToolStripMenuItem_Click (object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "GML|*.gml";

            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                System.IO.StreamReader sr = new
                System.IO.StreamReader(openFileDialog.FileName);
                String valuesString = sr.ReadToEnd();
                sr.Close();

                String[] firstSplit = valuesString.Split(',');

                solution = firstSplit[1];

                this.clearFields();
                int offset = 0;
                foreach (TextBox textBox in allFields) {
                    int value = firstSplit[0][2 * offset] - '0';
                    char state = firstSplit[0][(2 * offset) + 1];

                    if (value != 0) {
                        textBox.Text = value.ToString();

                        if (state == 'L') {
                            String newTag = textBox.Tag.ToString();
                            newTag = newTag.Replace('U', 'L');
                            textBox.Tag = newTag;
                            textBox.Enabled = false;
                            textBox.BackColor = Color.LightBlue;
                        }
                    }

                    offset++;
                }
            }
        }

        private void solutionToolStripMenuItem_Click (object sender, EventArgs e)
        {
            if (solution != "" && solution != null) {
                this.clearColors();

                int offset = 0;
                foreach (TextBox textBox in allFields) {
                    textBox.Text = solution[offset].ToString();
                    offset++;
                }
            } else {
                string message = "Board was not generated either load a board or generate new";
                string caption = "No board";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
            }
        }
    }
}
