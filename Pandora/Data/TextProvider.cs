using System;
using System.Collections;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Reflection;

namespace TheBox.Lang
{
	[ Serializable ]
	/// <summary>
	/// Provides localized text elements for the box
	/// </summary>
	public class TextProvider
	{
		/// <summary>
		/// Gets the text associated with the specified resource
		/// </summary>
		public string this[string description]
		{
			get
			{
				string[] locate = description.Split( new char[] { '.' } );

				if ( locate.Length != 2 )
				{
					return null;
				}

				Hashtable loc = (Hashtable) m_Sections[ locate[ 0 ] ];

				if ( loc == null )
					return null;

				return (string) loc[ locate[1] ];
			}
			set
			{
				string[] locate = description.Split( new char[] { '.' } );

				if ( locate.Length != 2 )
				{
					throw new Exception( "Bad descriptor when adding a new entry to text provider" );
				}

				Add( value, locate[0], locate[1] );
			}
		}

		private Hashtable m_Sections;
		private string m_Language;

		/// <summary>
		/// Gets or sets a string identifying the language represented by the text provider
		/// </summary>
		public string Language
		{
			get { return m_Language; }
			set { m_Language = value; }
		}

		/// <summary>
		/// Gets or sets the data collection for this text provider
		/// </summary>
		public Hashtable Data
		{
			get { return m_Sections; }
			set { m_Sections = value; }
		}

		/// <summary>
		/// Creates a new TextProvider object
		/// </summary>
		public TextProvider()
		{
			m_Sections = new Hashtable();
		}

		private void Add( string text, string category, string definition )
		{
			Hashtable loc = null;

			if ( m_Sections.ContainsKey( category ) )
			{
				loc = (Hashtable) m_Sections[ category ];
			}
			else
			{
				loc = new Hashtable();

				m_Sections.Add( category, loc );
			}

			loc[ definition ] = text;
		}

		/// <summary>
		/// Deletes a section contained in the TextProvider
		/// </summary>
		/// <param name="name">The name of the section that will be deleted</param>
		public void DeleteSection( string name )
		{
			m_Sections.Remove( name );
		}

		/// <summary>
		/// Removes an item from the TextProvider
		/// </summary>
		/// <param name="section">The section the item belongs to</param>
		/// <param name="item">The item name</param>
		public void RemoveItem( string section, string item )
		{
			Hashtable hash = (Hashtable) m_Sections[ section ];

			if ( hash != null )
			{
				hash.Remove( item );
			}
		}

		/// <summary>
		/// Removes an item from the TextProvider
		/// </summary>
		/// <param name="definition">The full item definition</param>
		public void RemoveItem( string definition )
		{
			string[] loc = definition.Split( new char[] { '.' } );

			if ( loc.Length != 2 )
				return;

			RemoveItem( loc[0], loc[1] );
		}

		/// <summary>
		/// Saves the contents of the TextProvider to file
		/// </summary>
		/// <param name="filename"></param>
		public void Serialize( string filename )
		{
			XmlDocument dom = new XmlDocument();

			XmlNode decl = dom.CreateXmlDeclaration( "1.0", null, null );

			dom.AppendChild( decl );

			XmlNode lang = dom.CreateElement( "Data" );

			XmlAttribute langtype = dom.CreateAttribute( "language" );
			langtype.Value = m_Language;
			lang.Attributes.Append( langtype );

			foreach ( string toplevel in m_Sections.Keys )
			{
				XmlNode topnode = dom.CreateElement( "section" );

				XmlAttribute topname = dom.CreateAttribute( "name" );
				topname.Value = toplevel;

				topnode.Attributes.Append( topname );

				Hashtable hash = ( (Hashtable) m_Sections[ toplevel ] );

				foreach( string lowlevel in hash.Keys )
				{
					XmlNode entrynode = dom.CreateElement( "entry" );

					XmlAttribute name = dom.CreateAttribute( "name" );
					name.Value = lowlevel;
					entrynode.Attributes.Append( name );

					XmlAttribute val = dom.CreateAttribute( "text" );
					val.Value = (string) hash[ lowlevel ];
					entrynode.Attributes.Append( val );

					topnode.AppendChild( entrynode );
				}
				
				lang.AppendChild( topnode );
			}

			dom.AppendChild( lang );

			dom.Save( filename );
		}

		/// <summary>
		/// Reads a TextProvider item from an Xml document
		/// </summary>
		/// <param name="dom">The XmlDocument containing the object</param>
		/// <returns>A TextProvider object</returns>
		public static TextProvider Deserialize( XmlDocument dom )
		{
			XmlNode data = dom.ChildNodes[ 1 ];

			TextProvider text = new TextProvider();

			text.m_Language = data.Attributes[ "language" ].Value;

			foreach ( XmlNode section in data.ChildNodes )
			{
				string topkey = section.Attributes[ "name" ].Value;

				Hashtable hash = new Hashtable();

				foreach ( XmlNode entry in section.ChildNodes )
				{
					string lowkey = entry.Attributes[ "name" ].Value;
					string t = entry.Attributes[ "text" ].Value;

					hash.Add( lowkey, t );
				}

				text.m_Sections.Add( topkey, hash );
			}

			return text;
		}

		/// <summary>
		/// Gets the language corresponding to the language selected in the profile
		/// </summary>
		/// <returns>A Text Provider object</returns>
		public static TextProvider GetLanguage()
		{
			return GetLanguage( Pandora.Profile.Language );
		}

		/// <summary>
		/// Gets the language corresponding to the currently selected language
		/// </summary>
		/// <returns></returns>
		public static TextProvider GetLanguage( string language )
		{
			string file = Path.Combine( Pandora.Folder, "Lang" );
			string resource = null;
			
			file = Path.Combine( file, string.Format( "{0}.dll", language ) );

			if ( ! File.Exists( file ) )
			{
				// Selected language doesn't exist. Revert to English
				System.Windows.Forms.MessageBox.Show( string.Format( "The langague selected for the current profile could not be located. English will be used instead.\n\nMissing language: {0}.", Pandora.Profile.Language ) );

				Pandora.Profile.Language = "English";

				file = Path.Combine( Pandora.Folder, "Lang" );
				file = Path.Combine( file, string.Format( "English.dll" ) );

				if ( !File.Exists( file ) )
				{
					// English doesn't exist either. This is wrong.
					System.Windows.Forms.MessageBox.Show( "Pandora's Box couldn't locate a required component (English.dll). Please reinstall the program to address this issue." );
					Pandora.Log.WriteError( null, "English.dll not found. Closing." );
					throw new Exception( "Default language file not found" );
				}
			}

			try
			{
				// Read the TextProvider object
				resource = string.Format( "{0}.language.xml", language );

				// Load the assembly
				Assembly asm = Assembly.LoadFile( file );
				Stream stream = asm.GetManifestResourceStream( resource );

				XmlDocument dom = new XmlDocument();
				dom.Load( stream );

				stream.Close();

				TextProvider tp = Deserialize( dom );

				return tp;
			}
			catch ( Exception err )
			{
				System.Windows.Forms.MessageBox.Show( "An unexpected error occurred when loading language files. Details about the error have been recorded in the log file. Pandora's Box will now close." );
				Pandora.Log.WriteError( err, "Loading resource {0} from assembly in file {1}", resource, file );
				throw new Exception( "Language file corrupted" );
			}
		}
	}
}