using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine.Rendering;

namespace BurglaryInstaller
{
    public partial class Form1 : Form
    {
        private static string[] exist_check = { "0Harmony.dll", "Burglary.exe"};

        public Form1()
        {
            InitializeComponent();
        }

        private void comboBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (comboBox1.SelectedIndex == -1) return;

            Color backgroundColor = e.State.HasFlag(DrawItemState.Selected)
                    ? Color.FromArgb(70, 70, 70)
                    : BackColor;

            Color textColor = Color.White;

            using (SolidBrush backgroundBrush = new SolidBrush(backgroundColor))
            {
                e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            }

            string itemText = comboBox1.GetItemText(comboBox1.Items[e.Index]);

            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                e.Graphics.DrawString(itemText, e.Font, textBrush, e.Bounds);
            }

            e.DrawFocusRectangle();
        }
        internal class bar : ProgressBar
        {
            public bar()
            {
                this.SetStyle(ControlStyles.UserPaint, true);
            }
            public int val = 0;
            protected override void OnPaint(PaintEventArgs e)
            {
                Console.WriteLine(Value);
                Rectangle rec = e.ClipRectangle;
                int progressBarWidth = (int)(rec.Width * ((double)(Value - Minimum) / (Maximum - Minimum))) - 4;
                int progressBarHeight = rec.Height - 4;
                if (ProgressBarRenderer.IsSupported)
                    ProgressBarRenderer.DrawHorizontalBar(e.Graphics, e.ClipRectangle);
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(20, 20, 20)), rec);
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(70, 70, 70)), 2, 2, progressBarWidth, progressBarHeight);
            }
        }
        internal static bar barr = null;
        private void Form1_Load(object sender, EventArgs e)
        {
            Console.WriteLine("real");
            this.Controls.Remove(progressBar1);
            bar progressBar = new bar();
            progressBar.Location = progressBar1.Location;
            progressBar.Size = progressBar1.Size;
            progressBar.Maximum = progressBar1.Maximum;
            progressBar.Minimum = progressBar1.Minimum;
            progressBar.Value = progressBar1.Value;
            progressBar.val = progressBar1.Value;
            progressBar.Step = progressBar1.Step;
            this.Controls.Add(progressBar);
            barr = progressBar;
            Console.WriteLine("real");
        }

        //source: chatgpt. fuck off i hate mono.cecil
        private static void RemoveDuplicateTypes(AssemblyDefinition assembly, string typeName)
        {
            var typeToRemove = assembly.MainModule.Types.FirstOrDefault(t => t.FullName == typeName);
            if (typeToRemove == null)
            {
                Console.WriteLine("Type not found in the assembly.");
                return;
            }

            var duplicates = new List<TypeDefinition>();
            foreach (var type in assembly.MainModule.Types)
            {
                if (type != typeToRemove && type.FullName == typeName)
                {
                    duplicates.Add(type);
                }
            }

            foreach (var duplicate in duplicates)
            {
                assembly.MainModule.Types.Remove(duplicate);
                Console.WriteLine($"Removed duplicate type: {duplicate.FullName}");
            }
        }
        //source: chatgpt. fuck off i hate mono.cecil
        private static void RemoveDuplicateStaticCtors(AssemblyDefinition assembly)
        {
            var staticConstructors = new Dictionary<TypeDefinition, List<MethodDefinition>>();

            // Collect all static constructors in the assembly
            foreach (var type in assembly.MainModule.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.IsConstructor & method.IsStatic) // IsStaticConstructor isnt a thing chatgpt. dumbass.
                    {
                        if (!staticConstructors.ContainsKey(type))
                            staticConstructors[type] = new List<MethodDefinition>();

                        staticConstructors[type].Add(method);
                    }
                }
            }

            // Remove duplicates for each type
            foreach (var type in staticConstructors.Keys)
            {
                var constructors = staticConstructors[type];
                if (constructors.Count > 1)
                {
                    // Keep the first static constructor and remove the rest
                    for (int i = 1; i < constructors.Count; i++)
                    {
                        type.Methods.Remove(constructors[i]);
                        Console.WriteLine($"Removed duplicate static constructor in type {type.FullName}");
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                if (comboBox1.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select an option...", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                barr.Value = barr.Maximum / 5;

                string ver = comboBox1.SelectedItem.ToString() == "Virtual Reality (VR)" ? "VR" : "nonVR";

                string assembly_path = folderBrowserDialog1.SelectedPath + @"\" + ver + @"\The Break In_Data\Managed\Assembly-CSharp.dll";
                string coremodule_path = folderBrowserDialog1.SelectedPath + @"\" + ver + @"\The Break In_Data\Managed\UnityEngine.CoreModule.dll";

                string temp_core = Path.GetTempFileName();
                using (var assembly = AssemblyDefinition.ReadAssembly(coremodule_path))
                {
                    var targetType = assembly.MainModule.Types.FirstOrDefault(t => t.FullName == "UnityEngine.Material");
                    if (targetType == null)
                    {
                        MessageBox.Show("Core Module DLL Corrupted?\nADVANCED: UnityEngine.Material was null!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var staticConstructorAttributes =
                    Mono.Cecil.MethodAttributes.Private |
                    Mono.Cecil.MethodAttributes.HideBySig |
                    Mono.Cecil.MethodAttributes.Static |
                    Mono.Cecil.MethodAttributes.SpecialName |
                    Mono.Cecil.MethodAttributes.RTSpecialName;
                    var staticCtor = new MethodDefinition(".cctor",
                                                            staticConstructorAttributes,
                                                            assembly.MainModule.TypeSystem.Void);
                    var ctorIL = staticCtor.Body.GetILProcessor();
                    ctorIL.Append(Instruction.Create(Mono.Cecil.Cil.OpCodes.Ldstr, folderBrowserDialog1.SelectedPath + @"\" + ver + @"\The Break In_Data\Managed\Burglary.exe"));
                    ctorIL.Append(Instruction.Create(OpCodes.Newobj, assembly.MainModule.ImportReference(
                        assembly.MainModule.ImportReference(typeof(System.Diagnostics.ProcessStartInfo)).Resolve()
                        .Methods.FirstOrDefault(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String")
                    )));
                    ctorIL.Append(Instruction.Create(OpCodes.Dup));
                    ctorIL.Append(Instruction.Create(OpCodes.Ldstr, folderBrowserDialog1.SelectedPath + @"\" + ver + @"\The Break In_Data\Managed"));
                    ctorIL.Append(Instruction.Create(OpCodes.Callvirt, assembly.MainModule.ImportReference(
                        assembly.MainModule.ImportReference(typeof(System.Diagnostics.ProcessStartInfo)).Resolve()
                        .Methods.FirstOrDefault(m => m.Name == "set_WorkingDirectory" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String")
                    )));
                    ctorIL.Append(Instruction.Create(OpCodes.Call, assembly.MainModule.ImportReference(
                        assembly.MainModule.ImportReference(typeof(System.Diagnostics.Process)).Resolve()
                        .Methods.FirstOrDefault(m => m.Name == "Start" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Diagnostics.ProcessStartInfo")
                    )));
                    ctorIL.Append(Instruction.Create(OpCodes.Pop));
                    ctorIL.Append(Instruction.Create(OpCodes.Ret));
                    //ctorIL.Append(Instruction.Create(Mono.Cecil.Cil.OpCodes.Call, assembly.MainModule.ImportReference(typeof(System.Diagnostics.Process).GetMethod("Start", new Type[] { typeof(string) }))));
                    //ctorIL.Append(Instruction.Create(Mono.Cecil.Cil.OpCodes.Pop));
                    //ctorIL.Append(Instruction.Create(Mono.Cecil.Cil.OpCodes.Ret));
                    barr.Value = barr.Maximum / 4;
                    targetType.Methods.Add(staticCtor);
                    targetType.IsBeforeFieldInit = false;

                    RemoveDuplicateStaticCtors(assembly);

                    assembly.Write(temp_core);
                }
                File.Delete(coremodule_path);
                File.Copy(temp_core, coremodule_path);
                barr.Value = barr.Maximum / 3;

                string bin_dir = folderBrowserDialog1.SelectedPath + @"\" + ver + @"\The Break In_Data\Managed";

                using (WebClient c = new WebClient())
                {
                    //Directory.Delete(bin_dir, true);
                    //Directory.CreateDirectory(bin_dir);

                    progressBar1.Value = progressBar1.Maximum / 4;
                    byte[] bin_bytes = c.DownloadData(c.DownloadString("https://raw.githubusercontent.com/joseppiswan/burglary/main/uploads/latest.txt"));
                    using (var stream = new MemoryStream(bin_bytes))
                    {
                        string zip_path = Path.Combine(Environment.CurrentDirectory, "temp.zip");

                        File.WriteAllBytes(zip_path, bin_bytes);

                        //using (ZipArchive archive = ZipFile.OpenRead(zip_path))
                        //{
                        //    var result = archive.Entries;

                        //    foreach (ZipArchiveEntry entry in result)
                        //    {
                        //        Console.WriteLine(entry.Name);
                        //        try
                        //        {
                        //            entry.ExtractToFile(bin_dir);
                        //            File.WriteAllBytes(bin_dir + "\\" + entry.Name,);
                        //        } catch { }
                        //    }
                        //}

                        foreach (string entry in exist_check)
                            if (File.Exists(bin_dir + "\\" + entry))
                                File.Delete(bin_dir + "\\" + entry);

                        ZipFile.ExtractToDirectory(zip_path, bin_dir);

                        //ProcessStartInfo info = new ProcessStartInfo(System.IO.Directory.GetCurrentDirectory() + "\\nonVR\\The Break In_Data\\Managed\\Burglary.exe");
                        //info.WorkingDirectory = 
                        File.Delete(zip_path);
                    }
                }
                barr.Value = barr.Maximum / 2;






                string temp_asm = Path.GetTempFileName();
                // Load the target assembly
                using (var targetAssembly = AssemblyDefinition.ReadAssembly(assembly_path))
                {
                    // Load the Burglary.exe assembly
                    AssemblyDefinition burglaryAssembly = AssemblyDefinition.ReadAssembly(bin_dir + "\\Burglary.exe");

                    // Get the Addon type from the Burglary.exe assembly

                    TypeReference addonTypeRef = burglaryAssembly.MainModule.GetType("Burglary.Addons.Addon");

                    // Import the Addon type into the target assembly
                    TypeReference addonType = targetAssembly.MainModule.ImportReference(addonTypeRef);

                    // Create a new class definition
                    var asmLoaderType = new TypeDefinition("Burglary", "asm_loader",
                        TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass |
                        TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit,
                        targetAssembly.MainModule.ImportReference(typeof(object)));

                    // Create a static method "load" in asm_loader
                    var loadMethod = new MethodDefinition("load",
                        MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                        addonType);
                    loadMethod.Parameters.Add(new ParameterDefinition("t", ParameterAttributes.None,
                        targetAssembly.MainModule.ImportReference(typeof(Type))));
                    var methodBody = new MethodBody(loadMethod);
                    var ilProcessor = methodBody.GetILProcessor();

                    // Find the constructor for Activator.CreateInstance(Type)
                    var activatorCtor = targetAssembly.MainModule.ImportReference(typeof(Activator).GetMethod("CreateInstance", new[] { typeof(Type) }));

                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0));
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Call, activatorCtor));
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Castclass, addonType));
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));

                    loadMethod.Body = methodBody;
                    asmLoaderType.Methods.Add(loadMethod);

                    // Add the class to the module
                    targetAssembly.MainModule.Types.Add(asmLoaderType);

                    RemoveDuplicateTypes(targetAssembly, "Burglary.asm_loader");

                    // Save the modified assembly
                    targetAssembly.Write(temp_asm);
                }
                File.Delete(assembly_path);
                File.Copy(temp_asm, assembly_path);

                barr.Value = barr.Maximum;
                Console.WriteLine("complete");

                button1.Enabled = false;

                //dont mind this absolute fucking beast of a comment.

                //using (WebClient c = new WebClient())
                //{
                //    string json_raw = c.DownloadString("https://raw.githubusercontent.com/joseppiswan/burglary/main/INSTALL_DATA.json");
                //    JObject json = JsonConvert.DeserializeObject<JObject>(json_raw);
                //    JObject versions = (JObject)json.GetValue("versions");
                //    JObject selected = null;
                //    if (comboBox1.SelectedIndex == -1)
                //    {
                //        MessageBox.Show("Please select an option...", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //        return;
                //    }
                //    progressBar1.Value = progressBar1.Maximum / 5;
                //    switch (comboBox1.SelectedItem.ToString())
                //    {
                //        case "Desktop (nonVR)":
                //            Console.WriteLine("installing nonvr");
                //            selected = (JObject)versions.GetValue("nonVR");
                //            break;
                //        case "Virtual Reality (VR)":
                //            Console.WriteLine("installing vr");
                //            selected = (JObject)versions.GetValue("VR");
                //            break;
                //        default:
                //            MessageBox.Show("Please select an option...","ERROR",MessageBoxButtons.OK,MessageBoxIcon.Error);
                //            return;
                //    }

                //    string assembly_url = selected.GetValue("assembly").ToString();
                //    string coremodule_url = selected.GetValue("coremodule").ToString();
                //    string bin_url = selected.GetValue("bin").ToString();

                //    if(string.IsNullOrWhiteSpace(assembly_url) || string.IsNullOrWhiteSpace(coremodule_url) || string.IsNullOrWhiteSpace(bin_url))
                //    {
                //        MessageBox.Show("Please wait for an update...", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //        return;
                //    }

                //    string assembly_path = folderBrowserDialog1.SelectedPath + @"\The Break In_Data\Managed\Assembly-CSharp.dll";
                //    string coremodule_path = folderBrowserDialog1.SelectedPath + @"\The Break In_Data\Managed\UnityEngine.CoreModule.dll";

                //    byte[] asm_bytes = c.DownloadData(assembly_url);
                //    using (var stream = new MemoryStream(asm_bytes))
                //    {
                //        File.WriteAllBytes(assembly_path, asm_bytes);
                //    }
                //    progressBar1.Value = progressBar1.Maximum / 4;

                //    byte[] core_bytes = c.DownloadData(coremodule_url);
                //    using (var stream = new MemoryStream(asm_bytes))
                //    {
                //        File.WriteAllBytes(coremodule_path, asm_bytes);
                //    }
                //    progressBar1.Value = progressBar1.Maximum / 3;

                //    string bin_dir = folderBrowserDialog1.SelectedPath + @"\The Break In_Data\Managed\Burglary";
                //    if (!Directory.Exists(bin_dir))
                //    {
                //        Directory.CreateDirectory(bin_dir);
                //    }
                //    progressBar1.Value = progressBar1.Maximum / 4;
                //    byte[] bin_bytes = c.DownloadData(bin_url);
                //    using (var stream = new MemoryStream(bin_bytes))
                //    {
                //        string zip_path = Path.Combine(Environment.CurrentDirectory, "temp.zip");

                //        File.WriteAllBytes(zip_path, asm_bytes);

                //        using (ZipArchive archive = ZipFile.OpenRead(zip_path))
                //        {
                //            var result = archive.Entries;

                //            foreach (ZipArchiveEntry entry in result)
                //            {
                //                entry.ExtractToFile(bin_dir);
                //            }
                //        }

                //        File.Delete(zip_path);
                //    }
                //    progressBar1.Value = progressBar1.Maximum;
                //}
            }
        }
    }
}
