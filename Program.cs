using System;
using System.IO;
using System.Text;
using System.Web;
using System.Net;
using System.IO.Compression;
using System.Windows.Forms;
using System.Collections.Generic;
using VideoConverter;
using Majestic12;

namespace f24
{
	/// <summary>
	/// Class with program entry point.
	/// </summary>
	internal sealed class Program
	{
		const string flv="flv", fe=".";
		static string fn, fmt, fdI, fdO, fnO;
		static VideoConverter.FFMpegConverter vc;
		static ConvertSettings vcs;
		static Majestic12.HTMLparser parser;
		/// <summary>
		/// Program entry point.
		/// </summary>
		[STAThread]
		private static void Main(string[] args)
		{
			//File.WriteAllBytes("bc",Convert.FromBase64String("vg23gr25u7ov2fjema8b0s5sb5as2ym5"));
			//var fvd=parseFlashVars(File.ReadAllText("phex0.js",Encoding.GetEncoding(1251)));
			vc=new FFMpegConverter();
			vc.FFMpegExeName ="c:\\progra~1\\Audacity\\FFmpeg\\ffmpeg.exe";
			vc.FFMpegToolPath ="c:\\progra~1\\Audacity\\FFmpeg";	
			vcs =new ConvertSettings();
			vc.ConvertProgress+= new EventHandler<ConvertProgressEventArgs>(vc_ConvertProgress);
			System.Diagnostics.Debug.WriteLine(args[1]);
			string bufl=Path.GetExtension(args[1]);
			bufl=args[1].Replace(bufl,"_0"+bufl);
			if(File.Exists(bufl)) File.Delete(bufl);
			File.Copy(args[1],bufl);
			string[] fnl=File.ReadAllLines(args[1],Encoding.GetEncoding(1251));
			fdI = Path.GetDirectoryName(fnl[0]);
			fdO = fdI;
			fnO = fdI+"\\"+Path.GetFileNameWithoutExtension(fnl[0]);
			fmt="mp4";
			switch(args[0])
			{
					case "flv": xFLV(fnl); break;
					case "cbu": xCBU(fnl); break;
					case "ac3": vcs.VideoCodec ="copy";	vcs.AudioBitRate="640k"; vcs.AudioCodec ="ac3"; xEnc(fnl); break;
					case "cat": xCAT(fnl); break;
					case "ogg": vcs.VideoBitRate="2400k"; vcs.VideoCodec ="libtheora"; vcs.AudioBitRate="192k"; vcs.AudioCodec ="libvorbis"; xEnc(fnl); break;
					case "av1": vcs.VideoBitRate="2400k"; vcs.VideoCodec ="libaom_av1"; vcs.AudioBitRate="192k"; vcs.AudioCodec ="libopus"; fmt="ogv"; xEnc(fnl); break;
					case "m3v": vcs.VideoCodec ="mpeg4"; vcs.AudioCodec ="libmp3lame"; vcs.CustomOutputArgs="-qscale 9"; xEnc(fnl); break;
					case "m4v": vcs.VideoCodec ="libx264"; vcs.AudioCodec ="aac"; vcs.CustomOutputArgs="-crf 30"; xEnc(fnl); break;
					case "m5v": vcs.VideoCodec ="libx265"; vcs.AudioCodec ="aac"; vcs.CustomOutputArgs="-crf 22"; xEnc(fnl); break;
					case "cpy": vcs.VideoCodec ="copy"; vcs.AudioCodec ="copy"; vcs.CustomInputArgs="-allowed_extensions ALL"; xEnc(fnl); break;
			}
		}

		static void xEnc(string[] fnl)	
		{
			for(int f=0;f<fnl.Length;f++)				
			{
				string fni=fnl[f];fn=fni;
				fdI=Path.GetDirectoryName(fni)+"\\"; fdO=fdI;
				if(fni.IndexOf("\\")!=-1) fn=Path.GetFileName(fni);
				if(!File.Exists(fdI+fn)) continue;
				long fz=new FileInfo(fdI+fn).Length;
				string dn=fdO+fn.Replace(Path.GetExtension(fn),"_out."+fmt);
				int x=f>1?".mp3.aac.m4a".IndexOf(Path.GetExtension(fnl[f+1])):-1;if(x==-1)
				{try{vc.ConvertMedia(fdI+fn,null,dn,fmt,vcs);}
				catch(FFMpegException e) {System.Diagnostics.Debug.WriteLine("Err{0}: {1}",e.ErrorCode, e.Message);	}}
				else{f++;fz+=new FileInfo(fnl[f]).Length;
				try{vc.ConvertMedia(new FFMpegInput[]{new FFMpegInput(fdI+fn),new FFMpegInput(fnl[f])},dn,fmt,vcs);}
				catch(FFMpegException e) {System.Diagnostics.Debug.WriteLine("Err{0}: {1}",e.ErrorCode, e.Message);	}}
				long f2=new FileInfo(dn).Length;
				System.Diagnostics.Debug.WriteLine("Delete {0}? {1}",fn,(f2*100)/fz);
				//if((f2*100)/fz>80) File.Delete(fdI+fn);
			}
		}
		static void xFLV(string[] fnl)
		{
			foreach(string fni in fnl)				
			{
				fn=fni;
				fdI=Path.GetDirectoryName(fni)+"\\"; fdO=fdI;
				if(fni.IndexOf("\\")!=-1) fn=Path.GetFileName(fni);
				if(!File.Exists(fdI+fn)) continue;
				long fz=new FileInfo(fdI+fn).Length;
				vcs.AudioCodec ="copy";
   				vcs.VideoCodec ="copy";
				int vcId=getCId(fdI+fn,"videocodecid");
				int acId=getCId(fdI+fn,"audiocodecid");				
				fmt="mp4"; if(vcId==0) {vcs.CustomOutputArgs="-crf 24";vcs.AudioCodec ="aac";vcs.VideoCodec="libx264";}
				else if(vcId!=0x1C40) fmt="avi";
			    if(vcId==0x1040) if(acId==0x1840) vcs.AudioCodec ="aac"; //fmt="mp4"; }
			    System.Diagnostics.Debug.WriteLine("{0} vCodecId:{1:x}, aCodecId:{2:x} is {3}",fn,vcId,acId, fmt);
			    try{  vc.ConvertMedia(fdI+fn,flv,fdO+fn.Replace(fe+flv,fe+fmt),fmt,vcs);}
				catch(FFMpegException e) {System.Diagnostics.Debug.WriteLine("Err{0}: {1}",e.ErrorCode, e.Message);	}
				if(File.Exists(fdO+fn.Replace(fe+flv,fe+fmt))) 
				{
					long f2=new FileInfo(fdO+fn.Replace(fe+flv,fe+fmt)).Length;
					System.Diagnostics.Debug.WriteLine("Delete {0}? {1}",fn,(f2*100)/fz);
					if((f2*100)/fz>80) File.Delete(fdI+fn);
				}
			}
		}
		static void xCBU(string[] fnl)
		{
            string iurl="";
			vcs.CustomInputArgs="-protocol_whitelist file,http,https,tcp,tls";
			if (Clipboard.ContainsText())
            {
                iurl = Clipboard.GetText();
                if (!IsValidUrl(iurl))
                    return;
                getCDU(ref iurl);
                iurl=iurl.Replace("%3D","=");
			}
			System.Diagnostics.Debug.WriteLine("in={0}:\n{1}",iurl,fnO);
			string fmt;
			fmt = "mp4";
			string fnp=Path.GetFileNameWithoutExtension(fnO);
			int o=fnp.Length;int z=o;while(o>0 && "0123456789".IndexOf(fnp.Substring(--o,1))>-1);
			int x=int.Parse(fnp.Substring(o+1))+1; 
			while(File.Exists(fdO+"\\"+fnp.Substring(0,o+1)+x++.ToString("000000".Substring(0,z-o-1))+fe+fmt));
			x--;fnO=fdO+"\\"+fnp.Substring(0,o+1)+x.ToString("000000".Substring(0,z-o-1))+fe+fmt;
			vcs.VideoCodec="copy"; vcs.AudioCodec="copy";
			try{  vc.ConvertMedia(iurl,null,fnO,fmt,vcs);}
			catch(FFMpegException e) {System.Diagnostics.Debug.WriteLine("Err{0}: {1}",e.ErrorCode, e.Message);	}
			StreamWriter sw=new StreamWriter (Path.GetDirectoryName(fnO)+"\\fc4cb.txt",true,Encoding.UTF8);
			sw.WriteLine(Path.GetFileName(fnO)+" from "+iurl); sw.Close();
		}
		static void getCDU(ref string pu)
		{
			var dcc=new Dictionary<string,string>();string dm="";
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			var sr=new StreamReader(assembly.GetManifestResourceStream("fcc.dcc"),Encoding.UTF8);
//			foreach(var s in File.ReadAllLines("fcc.dcc",Encoding.UTF8))
			string s; while(!sr.EndOfStream) {s=sr.ReadLine();
			int p=s.IndexOf('\t');dcc.Add(s.Substring(0,p),s.Substring(p+1));}
			foreach(var k in dcc.Keys) if(pu.IndexOf(k)>-1) {dm=k; break;}
			if(dm=="") return;
			byte[] by=getPUbytes(pu,dm,dcc[dm]);
			getM12(by); string v; int o=0;
			switch(dm) {
			  case "m24.ru":
		        //https://www.m24.ru/news/proisshestviya/20012020/104276
				v=getScrByID("type","application/ld+json");
				if(v !=null) {
                	o=v.IndexOf("contentUrl");
                	if(o>-1) pu=v.Substring(o+14,v.IndexOf(",",o+14)-o-15); }				
				break;}
			by=null;
			parser=null;
			return;
		}
		static string getScrByID(string pn,string pv)
		{
			string rv=null;
			HTMLchunk m12chunk = null;
	        while ((m12chunk = parser.ParseNext()) != null) {
	            switch (m12chunk.oType) {
	                case HTMLchunkType.OpenTag:break;
	                case HTMLchunkType.CloseTag:break;
	                case HTMLchunkType.Script:
	                if(pn=="") {int o=m12chunk.oHTML.IndexOf(pv);
	                	if(o>-1) rv=m12chunk.oHTML.Substring(o+pv.Length);}
	                else{
	                	if(m12chunk.GetParamValue(pn)==pv) rv=m12chunk.oHTML;}
	                break;
	                case HTMLchunkType.Comment:break;
	                case HTMLchunkType.Text:break;
	                default:break;}
        	}
			return rv;
		}
		static string getNPVByNm(string nn,string tv,string pn)
		{
			string rv=null;
			HTMLchunk m12chunk = null;
	        while ((m12chunk = parser.ParseNext()) != null) {
	            switch (m12chunk.oType) {
	                case HTMLchunkType.OpenTag:
					  if(m12chunk.sTag==nn)
	                    if(m12chunk.GetParamValue("name")==tv)
	                    	rv=m12chunk.GetParamValue(pn);
						break;
	                case HTMLchunkType.CloseTag:break;
	                case HTMLchunkType.Script:
	                break;
	                case HTMLchunkType.Comment:break;
	                case HTMLchunkType.Text:break;
	                default:break;}
        	}
			return rv;
		}
		static void getM12(byte[] by)
		{
	        parser = new HTMLparser();
	        parser.SetChunkHashMode(false);
	        parser.bKeepRawHTML = false;
	        parser.bDecodeEntities = true;
	        parser.bDecodeMiniEntities = true;
	        if (!parser.bDecodeEntities && parser.bDecodeMiniEntities)
	            parser.InitMiniEntities();
	        parser.bAutoExtractBetweenTagsOnly = true;
	        parser.bAutoKeepComments = true;
	        parser.bAutoKeepScripts = true;
	        parser.bCompressWhiteSpaceBeforeTag = true;
	        parser.bAutoMarkClosedTagsWithParamsAsOpen = false;
	        parser.Init(by);
		}
		static byte[] getPUbytes(string pu,string dm, string cu)
		{
	        HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(pu);
	        wr.Method = "GET";wr.Accept	="text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";wr.UserAgent="Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 UBrowser/7.0.185.1002 Safari/537.36";
	        wr.Headers.Add("Accept-Encoding","gzip,deflate,br"); wr.CookieContainer = new CookieContainer();
	        foreach(string cv in cu.Split(new char[]{';'})){
	        	string[] cva=cv.Split(new char[]{'='}); if(cva.Length==2) {var cc=new Cookie(cva[0].Trim(),cva[1],"",dm); wr.CookieContainer.Add(cc);}}
	        HttpWebResponse rsp = (HttpWebResponse)wr.GetResponse();
	        if(rsp.StatusCode!=HttpStatusCode.OK)
	        {System.Diagnostics.Debug.WriteLine(rsp.StatusCode+rsp.StatusDescription);return null;}
	        byte[] by=new byte[0x1000000];int p=0;
	        var vs=rsp.GetResponseStream();
	        while(true) {int bv=vs.ReadByte(); if(bv==-1) break; by[p++]=(byte)bv; }        
			wr=null;
			byte[] rb=new byte[p]; Array.Copy(by,rb,p); by=null; 
			if(rb[0]==0x1f)
			{MemoryStream ms=new MemoryStream();
			new GZipStream(new MemoryStream(rb),CompressionMode.Decompress).CopyTo(ms); rb=ms.ToArray();}
			var fw=new FileStream("t000.htm",FileMode.Create);fw.Write(rb,0,rb.Length);fw.Close();return rb;
		}
		static bool IsValidUrl(string txt)
	    {
	        Uri uriResult;
	        return Uri.TryCreate(txt, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
	    }
		static void xCAT(string[] fnl)
		{
			const string mf="mp4", fe=".";
			ConcatSettings vcc=new ConcatSettings();
			vcc.AudioCodec="copy";vcc.ConcatAudioStream=true;
			vcc.VideoCodec="copy";vcc.ConcatVideoStream=true;
			System.Diagnostics.Debug.WriteLine("Lin={0}: {1:d3}",fnO,fnl.Length);
			string fmt; long fz=0;
			//System.Diagnostics.Debug.WriteLine("{0}",fnl[0]);
			fmt=Path.GetExtension(fnl[0]);
			if (fmt != "avi") fmt = "mp4";
			int oc=0;
			string[] fol=new string[fnl.Length];
			foreach(string fni in fnl)
			{
				if(File.Exists(fni))
				{
					fol[oc++]=fni;
				   	fz+=new FileInfo(fni).Length;
				}
			}
			fnl=new string[oc];
			Array.Copy(fol,fnl,oc);
			System.Diagnostics.Debug.WriteLine("Written {0}: {1} bytes",fnl[0].Replace(fe+mf,fe+"mfl"),fz);
			string fnc=File.Exists(fnO+fe+mf)?"_c":"";
			try{  vc.ConcatMedia(fnl,fnO+fnc+fe+mf,fmt,vcc);}
			catch(FFMpegException e) {System.Diagnostics.Debug.WriteLine("Err{0}: {1}",e.ErrorCode, e.Message);	}
			long f2=new FileInfo(fnO+fe+fnc+mf).Length;
			System.Diagnostics.Debug.WriteLine("Delete {0}? {1}",fn,(f2*100)/fz);
			if((f2*100)/fz>80) foreach (string fni in fnl)
			{
				File.Delete(fni + fn);
			 }
		}
		static int getCId(string fn, string cit)
		{
			byte[] by = new Byte[0x10000]; byte[] pa=Encoding.ASCII.GetBytes(cit);
	        byte[] se = new byte[pa.Length];
			FileStream fs=new FileStream(fn,FileMode.Open,FileAccess.Read);
			fs.Read(by,0,by.Length);fs.Close();
			int vcId=0, o, p=0, i=Array.IndexOf<byte>(by, pa[0], p);
		    while (i >= 0)  
		    {
		      p=i; o=1;
		      while(by[p+o]==pa[o]) if(++o>pa.Length-1) break;
		      if(o==pa.Length)
		      {
		      	vcId=BitConverter.ToInt32(by, p+o+1);
		      	break;
		      }
		      i = Array.IndexOf<byte>(by, pa[0], i + 1);
		    }
		    return vcId;
		}
		static void vc_ConvertProgress(object sender, ConvertProgressEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine("{0}: {1:d2}",fn,(int)(100 * e.Processed.TotalSeconds /e.TotalDuration.TotalSeconds));
		}
		/*
		public static List<int> IndexOfSequence(this byte[] buffer, byte[] pattern, int startIndex)
		{
		   List<int> positions = new List<int>();
		   int i = Array.IndexOf<byte>(buffer, pattern[0], startIndex);  
		   while (i >= 0 && i <= buffer.Length - pattern.Length)  
		   {
		      byte[] segment = new byte[pattern.Length];
		      Buffer.BlockCopy(buffer, i, segment, 0, pattern.Length);    
		      if (segment.SequenceEqual<byte>(pattern))
		           positions.Add(i);
		      i = Array.IndexOf<byte>(buffer, pattern[0], i + 1);
		   }
		   return positions;    
		}*/
		public static string parseFlashVars(string v)
		{
			int o,p=0;string pu="";v=v.Substring(v.IndexOf("var "));
				for(int co=v.IndexOf("/*",p);co>-1;co=v.IndexOf("/*",p))
				{pu+=v.Substring(p,co-p);p=v.IndexOf("*/",p)+2;}
				pu+=v.Substring(p,v.Length-p);
			var QItems= new Dictionary<string,string>();
				var fVars= new Dictionary<string,string>();
				foreach (var e in pu.Replace("\n","").Substring(0,pu.IndexOf("qualityItems_")-5).Split(new char[]{';'}))
	         	if(e.Substring(0,4)=="var ") {
		         	p=e.IndexOf("=",5);o=e.IndexOf("+",5);string cc="";if(o==-1) cc=e.Substring(p+2,e.Length-p-3); 
	         		else 
	         			foreach(string t in e.Substring(p+1).Split(new string[]{" + "},StringSplitOptions.None)){
	         		if(t.Substring(0,1)=="\"")cc+=t.Replace("\"",""); else cc+=fVars[t];}
	         		if(fVars.ContainsKey(e.Substring(4,p-4))) fVars[e.Substring(4,p-4)]=cc; else fVars.Add(e.Substring(4,p-4),cc);}
				else {p=e.IndexOf("=");fVars.Add(e.Substring(0,p-1),e.Substring(p+1));}
				//var sw=new StreamWriter("phOut.csv",false,Encoding.GetEncoding(1251));
				//foreach(var kp in fVars) sw.WriteLine(String.Format("{0}\t{1}",kp.Key,kp.Value));sw.Close();
				pu="";foreach(var t in "1080p;720p;480p;360p;240p".Split(new char[]{';'}))if(fVars.ContainsKey("quality_"+t)){pu=fVars["quality_"+t];break;}
				return pu;
		}
	}
}
