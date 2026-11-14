using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;

[ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IDesktopWallpaper {
 void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitor,[MarshalAs(UnmanagedType.LPWStr)] string path);
 void GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string id,[MarshalAs(UnmanagedType.LPWStr)] out string path); void GetMonitorDevicePathAt(uint index,[MarshalAs(UnmanagedType.LPWStr)] out string id); void GetMonitorDevicePathCount(out uint count); [PreserveSig] int GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string id,out Rectangle rect); void SetBackgroundColor(); void GetBackgroundColor(); void SetPosition(int position); void GetPosition(out int position);
 void SetSlideshow([MarshalAs(UnmanagedType.IUnknown)] object items);
 void GetSlideshow();
 void SetSlideshowOptions(uint options,uint milliseconds);
 void GetSlideshowOptions(out uint options,out uint milliseconds); void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitor,int direction); void GetStatus(out uint state);
}
class Gallery : Form {
 [DllImport("shell32.dll",CharSet=CharSet.Unicode,PreserveSig=false)] static extern void SHParseDisplayName(string path,IntPtr context,out IntPtr pidl,uint attr,IntPtr attrs);
 [DllImport("shell32.dll",PreserveSig=false)] static extern void SHCreateShellItemArrayFromIDLists(uint count,IntPtr[] pidls,[MarshalAs(UnmanagedType.IUnknown)] out object items);
 readonly IDesktopWallpaper desktop=(IDesktopWallpaper)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD")));

 readonly string folder=Path.Combine(Application.StartupPath,"Cuadros");
 readonly ListView library=new ListView();
 readonly ImageList thumbs=new ImageList();
 readonly PictureBox preview=new PictureBox();
 readonly TextBox nameBox=new TextBox();
 readonly ComboBox monitors=new ComboBox();
 readonly NumericUpDown seconds=new NumericUpDown();
 readonly Label status=new Label();
 readonly Label countLabel=new Label();
 readonly List<string> monitorIds=new List<string>();
 readonly Color panel=Color.FromArgb(36,39,46), ink=Color.FromArgb(236,237,241), accent=Color.FromArgb(240,193,121);
 string Selected { get {return library.SelectedItems.Count==0?null:(string)library.SelectedItems[0].Tag;} }
 static bool Supported(string path){return Array.IndexOf(new[]{".jpg",".jpeg",".png",".bmp"},Path.GetExtension(path).ToLowerInvariant())>=0;}
 public Gallery(){
  Text="Backdrop Studio for Windows";Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(24,26,31);ForeColor=ink;ClientSize=new Size(1120,720);MinimumSize=new Size(900,620);StartPosition=FormStartPosition.CenterScreen;
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(16),ColumnCount=2,RowCount=3};
  layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,320));
  foreach(int h in new[]{59,0,30})layout.RowStyles.Add(h==0?new RowStyle(SizeType.Percent,100):new RowStyle(SizeType.Absolute,h));
  Controls.Add(layout);
  var header=new FlowLayoutPanel{Dock=DockStyle.Fill};header.Controls.Add(new Label{Text=Text,Font=new Font("Segoe UI Semibold",21),AutoSize=true});countLabel.AutoSize=true;countLabel.Margin=new Padding(15,16,0,0);header.Controls.Add(countLabel);layout.Controls.Add(header,0,0);layout.SetColumnSpan(header,2);
  library.Dock=DockStyle.Fill;library.BackColor=panel;library.ForeColor=ink;library.BorderStyle=BorderStyle.None;library.View=View.LargeIcon;library.MultiSelect=false;library.HideSelection=false;library.ShowItemToolTips=true;
  thumbs.ColorDepth=ColorDepth.Depth32Bit;thumbs.ImageSize=new Size(152,86);IntPtr imageHandle=thumbs.Handle;library.LargeImageList=thumbs;layout.Controls.Add(library,0,1);
  var side=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(10,0,0,0)};
  layout.Controls.Add(side,1,1);
  var drop=Button("＋  Añadir o arrastrar imágenes",PickFiles,true);drop.Width=280;drop.Height=49;side.Controls.Add(drop);
  foreach(Control area in new Control[]{drop,library,this}){area.AllowDrop=true;area.DragEnter+=delegate(object o,DragEventArgs e){if(e.Data.GetDataPresent(DataFormats.FileDrop))e.Effect=DragDropEffects.Copy;};area.DragDrop+=delegate(object o,DragEventArgs e){Import((string[])e.Data.GetData(DataFormats.FileDrop));};}
  preview.Width=280;preview.Height=146;preview.BackColor=Color.FromArgb(16,17,20);preview.SizeMode=PictureBoxSizeMode.Zoom;side.Controls.Add(preview);
  side.Controls.Add(Caption("Nombre del fondo"));nameBox.Width=280;side.Controls.Add(nameBox);side.Controls.Add(Button("Guardar nombre",Rename,false));
  nameBox.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;Run(Rename);}};
  library.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.F2){nameBox.Focus();nameBox.SelectAll();e.Handled=true;}};
  var destinations=new TabControl{Width=280,Height=210,AccessibleName="Destino del fondo"};
  var desktopTab=new TabPage("Escritorio"){BackColor=panel,ForeColor=ink};
  var lockTab=new TabPage("Bloqueo"){BackColor=panel,ForeColor=ink};
  destinations.TabPages.Add(desktopTab);destinations.TabPages.Add(lockTab);side.Controls.Add(destinations);
  var mode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Left=8,Top=9,Width=250,AccessibleName="Tipo de fondo del escritorio"};
  mode.Items.AddRange(new object[]{"Imagen fija","Carrusel aleatorio"});
  desktopTab.Controls.Add(mode);
  var fixedControls=new Panel{Left=8,Top=45,Width=255,Height=123};
  var slideControls=new Panel{Left=8,Top=45,Width=255,Height=123};
  desktopTab.Controls.Add(fixedControls);desktopTab.Controls.Add(slideControls);
  monitors.Width=250;monitors.DropDownStyle=ComboBoxStyle.DropDownList;fixedControls.Controls.Add(monitors);LoadMonitors();
  var apply=Button("Aplicar imagen al escritorio",delegate{RequireSelection();desktop.SetWallpaper(monitorIds[monitors.SelectedIndex],Selected);status.Text="Imagen aplicada al escritorio · "+monitors.Text;},true);
  apply.Top=40;fixedControls.Controls.Add(apply);
  var scope=Caption("Todas las pantallas del escritorio");scope.Location=new Point(0,0);slideControls.Controls.Add(scope);
  var every=Caption("Cambiar cada");every.Location=new Point(0,35);slideControls.Controls.Add(every);
  seconds.SetBounds(92,30,74,27);seconds.Minimum=1;seconds.Maximum=86400;uint flags,ms;desktop.GetSlideshowOptions(out flags,out ms);seconds.Value=Math.Max(1,Math.Min(86400,ms/1000));slideControls.Controls.Add(seconds);
  var unit=Caption("segundos");unit.Location=new Point(171,35);slideControls.Controls.Add(unit);
  var rotate=Button("Aplicar carrusel al escritorio",StartShuffle,true);rotate.Top=68;slideControls.Controls.Add(rotate);
  mode.SelectedIndexChanged+=delegate{fixedControls.Visible=mode.SelectedIndex==0;slideControls.Visible=mode.SelectedIndex==1;};
  uint state;desktop.GetStatus(out state);mode.SelectedIndex=(state&2)!=0?1:0;
  var lockMode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Left=8,Top=9,Width=250,AccessibleName="Tipo de fondo del bloqueo"};
  lockMode.Items.AddRange(new object[]{"Solo esta imagen","Carrusel aleatorio (todas las imágenes)"});lockTab.Controls.Add(lockMode);
  var lockFixed=Button("Aplicar esta imagen al bloqueo",delegate{RequireSelection();ChangeLock(Selected,0);status.Text="Imagen fija aplicada al bloqueo.";},true);
  lockFixed.Location=new Point(8,52);lockTab.Controls.Add(lockFixed);
  var rotation=new Panel{Left=8,Top=44,Width=254,Height=124};lockTab.Controls.Add(rotation);
  var rotationScope=Caption("Todas las imágenes de la biblioteca");rotationScope.Location=new Point(0,0);rotation.Controls.Add(rotationScope);
  var delayLabel=Caption("Cambiar cada");delayLabel.Location=new Point(0,35);rotation.Controls.Add(delayLabel);
  var lockSeconds=new NumericUpDown{Minimum=1,Maximum=86400};int saved=LockInterval();
  lockSeconds.Value=saved>0?saved:seconds.Value;lockSeconds.SetBounds(92,30,74,27);rotation.Controls.Add(lockSeconds);
  var lockUnit=Caption("segundos");lockUnit.Location=new Point(171,35);rotation.Controls.Add(lockUnit);
  var lockRotate=Button("Aplicar carrusel al bloqueo",delegate{ChangeLock(null,(int)lockSeconds.Value);status.Text="Carrusel de toda la biblioteca aplicado al bloqueo.";},true);
  lockRotate.Top=68;rotation.Controls.Add(lockRotate);
  lockMode.SelectedIndexChanged+=delegate{lockFixed.Visible=lockMode.SelectedIndex==0;rotation.Visible=lockMode.SelectedIndex==1;};
  lockMode.SelectedIndex=saved>0?1:0;
  status.Dock=DockStyle.Fill;status.ForeColor=accent;layout.Controls.Add(status,0,2);layout.SetColumnSpan(status,2);
  library.SelectedIndexChanged+=delegate{if(Selected!=null){using(var source=Image.FromFile(Selected)){var old=preview.Image;preview.Image=new Bitmap(source);if(old!=null)old.Dispose();}nameBox.Text=Path.GetFileNameWithoutExtension(Selected);}};
  Directory.CreateDirectory(folder); foreach(var path in Directory.GetFiles(folder)){if(Supported(path))AddTile(path);}
  Count();if(library.Items.Count>0)library.Items[0].Selected=true;
 }
 Label Caption(string s){return new Label{Text=s,AutoSize=true,Margin=new Padding(4,7,8,4)};}
 Button Button(string label,Action action,bool primary){var b=new Button{Text=label,AutoSize=true,Height=31,FlatStyle=FlatStyle.Flat,BackColor=primary?accent:panel,ForeColor=primary?Color.Black:ink,Cursor=Cursors.Hand};b.FlatAppearance.BorderSize=0;b.Click+=delegate{Run(action);};return b;}
 void Run(Action action){try{action();}catch(Exception e){status.Text=e.Message;}}
 void RequireSelection(){if(Selected==null)throw new InvalidOperationException("Selecciona un fondo.");}
 void Count(){countLabel.Text=library.Items.Count+" fondos";}
 void LoadMonitors(){
  monitorIds.Clear();monitors.Items.Clear();monitorIds.Add(null);monitors.Items.Add("Todas las pantallas");uint count;desktop.GetMonitorDevicePathCount(out count);
  for(uint i=0;i<count;i++){string id;Rectangle rect;desktop.GetMonitorDevicePathAt(i,out id);if(desktop.GetMonitorRECT(id,out rect)!=0)continue;monitorIds.Add(id);monitors.Items.Add("Pantalla "+monitorIds.IndexOf(id)+" ("+(rect.Width-rect.X)+" × "+(rect.Height-rect.Y)+")");}
  monitors.SelectedIndex=monitorIds.Count>1?1:0;
 }
 void AddTile(string path){
  using(var source=Image.FromFile(path))using(var tile=new Bitmap(152,86))using(var g=Graphics.FromImage(tile)){
   g.Clear(panel);g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
   double scale=Math.Min(152.0/source.Width,86.0/source.Height);int w=Math.Max(1,(int)(source.Width*scale)),h=Math.Max(1,(int)(source.Height*scale));g.DrawImage(source,(152-w)/2,(86-h)/2,w,h);thumbs.Images.Add(tile);
  }
  library.Items.Add(new ListViewItem(Path.GetFileNameWithoutExtension(path),thumbs.Images.Count-1){Tag=path,ToolTipText=Path.GetFileName(path)});
 }
 void PickFiles(){using(var dlg=new OpenFileDialog{Multiselect=true,Filter="Imágenes|*.jpg;*.jpeg;*.png;*.bmp"}){if(dlg.ShowDialog(this)==DialogResult.OK)Import(dlg.FileNames);}}
 void Import(string[] files){
  int added=0,skipped=0;string lastError="";
  foreach(var file in files){string temp=null;try{
   if(!Supported(file)||!File.Exists(file)){skipped++;continue;}
   string target=Path.Combine(folder,Path.GetFileName(file));if(string.Equals(target,file,StringComparison.OrdinalIgnoreCase)){skipped++;continue;}
   for(int i=2;File.Exists(target);i++)target=Path.Combine(folder,Path.GetFileNameWithoutExtension(file)+" ("+i+")"+Path.GetExtension(file));
   temp=Path.Combine(folder,Guid.NewGuid()+".tmp");File.Copy(file,temp);
   using(var img=Image.FromFile(temp)){if(img.Width<1)throw new IOException("Imagen vacía.");}
   File.Move(temp,target);temp=null;AddTile(target);added++;
  }catch(Exception ex){skipped++;lastError=ex.Message;}finally{if(temp!=null && File.Exists(temp))File.Delete(temp);}}
  Count();status.Text="Añadidas: "+added+" · Omitidas: "+skipped+"  "+lastError;
  if(added>0){var item=library.Items[library.Items.Count-1];item.Selected=true;item.EnsureVisible();}
 }
 void Rename(){
  RequireSelection();string name=nameBox.Text.Trim(),old=Selected;
  if(name.Length==0||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0||name.EndsWith("."))throw new IOException("Escribe un nombre válido.");
  string target=Path.Combine(folder,name+Path.GetExtension(old));if(target==old)return;
  if(!string.Equals(old,target,StringComparison.OrdinalIgnoreCase)&&File.Exists(target))throw new IOException("Ya existe otro fondo con ese nombre.");
  File.Move(old,target);var tile=library.SelectedItems[0];tile.Tag=target;tile.Text=name;tile.ToolTipText=Path.GetFileName(target);status.Text="Nombre guardado.";library.Focus();
 }
 const string SettingsKey=@"HKEY_CURRENT_USER\Software\FondosDePantallas";
 static readonly Random rng=new Random();
 static int LockInterval(){return Convert.ToInt32(Microsoft.Win32.Registry.GetValue(SettingsKey,"IntervaloBloqueo",0));}
 static List<string> Images(){return new List<string>(Array.FindAll(Directory.GetFiles(Path.Combine(Application.StartupPath,"Cuadros")),Supported));}
 static void ApplyLock(string path){
  var info=new ProcessStartInfo("powershell.exe","-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \""+Path.Combine(Application.StartupPath,"Bloqueo.ps1")+"\" \""+path+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true};
  using(var p=Process.Start(info)){string err=p.StandardError.ReadToEnd();p.WaitForExit();if(p.ExitCode!=0)throw new IOException(err.Trim());}
 }
 static void ChangeLock(string fixedImage,int interval){
  using(var gate=new System.Threading.Mutex(false,@"Local\FondosPantallasLockWrite")){
   Enter(gate);try{
    var files=Images();if(interval>0 && files.Count==0)throw new IOException("Añade imágenes a la biblioteca.");
    string first=interval>0?files[rng.Next(files.Count)]:fixedImage;ApplyLock(first);
    using(var run=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")){
     if(interval>0)run.SetValue("FondosDePantallasBloqueo","\""+Application.ExecutablePath+"\" --rotar-bloqueo");else run.DeleteValue("FondosDePantallasBloqueo",false);
    }
    Microsoft.Win32.Registry.SetValue(SettingsKey,"PrimerFondo",first);
    Microsoft.Win32.Registry.SetValue(SettingsKey,"Ciclo",Guid.NewGuid().ToString());
    Microsoft.Win32.Registry.SetValue(SettingsKey,"IntervaloBloqueo",interval);
    if(interval>0)Process.Start(Application.ExecutablePath,"--rotar-bloqueo");
   }finally{gate.ReleaseMutex();}
  }
 }
 static void Enter(System.Threading.Mutex gate){try{gate.WaitOne();}catch(System.Threading.AbandonedMutexException){}}
 static void Rotate(){
  using(var single=new System.Threading.Mutex(false,@"Local\FondosPantallasLockWorker"))
  using(var gate=new System.Threading.Mutex(false,@"Local\FondosPantallasLockWrite")){
   try{if(!single.WaitOne(0))return;}catch(System.Threading.AbandonedMutexException){}
   var used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);string cycle="",last="";DateTime due=DateTime.UtcNow;
   while(true){
    Enter(gate);try{
     int interval=LockInterval();if(interval<=0)return;
     string changed=(string)Microsoft.Win32.Registry.GetValue(SettingsKey,"Ciclo","");
     if(changed!=cycle){cycle=changed;last=(string)Microsoft.Win32.Registry.GetValue(SettingsKey,"PrimerFondo","");used.Clear();used.Add(last);due=DateTime.UtcNow.AddSeconds(interval);}
     if(DateTime.UtcNow>=due){
      var files=Images();var remaining=files.FindAll(p=>!used.Contains(p));
      if(remaining.Count==0){used.Clear();remaining=files.FindAll(p=>files.Count==1||p!=last);}
      if(remaining.Count>0){string chosen=remaining[rng.Next(remaining.Count)];ApplyLock(chosen);used.Add(chosen);last=chosen;}
      due=DateTime.UtcNow.AddSeconds(interval);
     }
    }catch(IOException){due=DateTime.UtcNow.AddSeconds(10);}finally{gate.ReleaseMutex();}
    System.Threading.Thread.Sleep(1000);
   }
  }
 }
 void StartShuffle(){
  IntPtr id;SHParseDisplayName(folder,IntPtr.Zero,out id,0,IntPtr.Zero);object items=null;
  try{SHCreateShellItemArrayFromIDLists(1,new[]{id},out items);int pos;desktop.GetPosition(out pos);if(pos==5)desktop.SetPosition(4);desktop.SetSlideshow(items);desktop.SetSlideshowOptions(1,(uint)seconds.Value*1000);
   LoadMonitors();var used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach(string monitor in monitorIds){if(monitor==null)continue;string path;desktop.GetWallpaper(monitor,out path);for(int n=0;used.Contains(path)&&n<library.Items.Count;n++){desktop.AdvanceSlideshow(monitor,0);desktop.GetWallpaper(monitor,out path);}used.Add(path);}
   status.Text="Carrusel aleatorio activo. Puedes cerrar la aplicación.";
  }finally{if(items!=null)Marshal.ReleaseComObject(items);Marshal.FreeCoTaskMem(id);}
 }
 [STAThread] static void Main(string[] args){if(args.Length==1 && args[0]=="--rotar-bloqueo"){Rotate();return;}Application.EnableVisualStyles();Application.Run(new Gallery());}
}





