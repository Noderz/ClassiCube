﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.IO;
using System.Text;
using ClassicalSharp.Renderers;

namespace ClassicalSharp.Commands {
	
	public sealed class CommandsCommand : Command {
		
		public override string Name {
			get { return "Commands"; }
		}
		
		public override string[] Help {
			get {
				return new [] {
					"&a/client commands",
					"Prints a list of all usable commands",
				};
			}
		}
		
		public override void Execute( CommandReader reader ) {
			List<string> commandNames = new List<string>();
			StringBuilder buffer = new StringBuilder( 64 );
			foreach( Command cmd in Window.CommandManager.RegisteredCommands ) {
				string name = cmd.Name;
				if( buffer.Length + name.Length > 64 ) {
					commandNames.Add( buffer.ToString() );
					buffer.Length = 0;
				}
				buffer.Append( name + ", " );
			}
			if( buffer.Length > 0 ) {
				commandNames.Add( buffer.ToString() );
			}
			foreach( string part in commandNames ) {
				Window.AddChat( part );
			}
		}
	}
	
	public sealed class EnvCommand : Command {
		
		public override string Name {
			get { return "Env"; }
		}
		
		public override string[] Help {
			get {
				return new [] {
					"&a/client env [property] [value]",
					"&bproperties: &eskycol, fogcol, cloudscol, cloudsspeed,",
					"     &esuncol, shadowcol",
					"&bcol properties: &evalue must be a hex colour code (either #RRGGBB or RRGGBB).",
					"&bcloudsspeed: &evalue must be a decimal number. (e.g. 0.5, 1, 4.5).",
					"&eIf no args are given, the current env variables will be displayed.",
				};
			}
		}
		
		public override void Execute( CommandReader reader ) {
			string property = reader.Next();
			if( property == null ) {
				Window.AddChat( "Fog colour: " + Window.Map.FogCol.ToRGBHexString() );
				Window.AddChat( "Clouds colour: " + Window.Map.CloudsCol.ToRGBHexString() );
				Window.AddChat( "Sky colour: " + Window.Map.SkyCol.ToRGBHexString() );
			} else if( property.Equals( "skycol", StringComparison.OrdinalIgnoreCase ) ) {
				ReadHexColourAnd( reader, c => Window.Map.SetSkyColour( c ) );
			} else if( property.Equals( "fogcol", StringComparison.OrdinalIgnoreCase ) ) {
				ReadHexColourAnd( reader, c => Window.Map.SetFogColour( c ) );
			} else if( property.Equals( "cloudscol", StringComparison.OrdinalIgnoreCase ) ||
			          property.Equals( "cloudcol", StringComparison.OrdinalIgnoreCase ) ) {
				ReadHexColourAnd( reader, c => Window.Map.SetCloudsColour( c ) );
			} else if( property.Equals( "suncol", StringComparison.OrdinalIgnoreCase ) ) {
				ReadHexColourAnd( reader, c => Window.Map.SetSunlight( c ) );
			} else if( property.Equals( "shadowcol", StringComparison.OrdinalIgnoreCase ) ) {
				ReadHexColourAnd( reader, c => Window.Map.SetShadowlight( c ) );
			} else if( property.Equals( "cloudsspeed", StringComparison.OrdinalIgnoreCase ) ||
			          property.Equals( "cloudspeed", StringComparison.OrdinalIgnoreCase ) ) {
				float speed;
				if( !reader.NextFloat( out speed ) ) {
					Window.AddChat( "&e/client env: &cInvalid clouds speed." );
				} else {
					StandardEnvRenderer env = Window.EnvRenderer as StandardEnvRenderer;
					if( env != null ) {
						env.CloudsSpeed = speed;
					}
				}
			}
		}
		
		void ReadHexColourAnd( CommandReader reader, Action<FastColour> action ) {
			FastColour colour;
			if( !reader.NextHexColour( out colour ) ) {
				Window.AddChat( "&e/client env: &cInvalid hex colour." );
			} else {
				action( colour );
			}
		}
	}
	
	public sealed class HelpCommand : Command {
		
		public override string Name {
			get { return "Help"; }
		}
		
		public override string[] Help {
			get {
				return new [] {
					"&a/client help [command name]",
					"&eDisplays the help (if available) for the given command.",
				};
			}
		}
		
		
		public override void Execute( CommandReader reader ) {
			string commandName = reader.Next();
			if( commandName == null ) {
				Window.AddChat( "&e/client: &cYou didn't specify a command to get help with." );
			} else {
				Command cmd = Window.CommandManager.GetMatchingCommand( commandName );
				if( cmd != null ) {
					string[] help = cmd.Help;
					for( int i = 0; i < help.Length; i++ ) {
						Window.AddChat( help[i] );
					}
				}
			}
		}
	}
	
	public sealed class InfoCommand : Command {
		
		public override string Name {
			get { return "Info"; }
		}
		
		public override string[] Help {
			get {
				return new [] {
					"&a/client info [property]",
					"&bproperties: &epos, target, dimensions, jumpheight",
				};
			}
		}
		
		
		public override void Execute( CommandReader reader ) {
			string property = reader.Next();
			if( property == null ) {
				Window.AddChat( "&e/client info: &cYou didn't specify a property." );
			} else if( property == "pos" ) {
				Window.AddChat( "Feet: " + Window.LocalPlayer.Position );
				Window.AddChat( "Eye: " + Window.LocalPlayer.EyePosition );
			} else if( property == "target" ) {
				PickedPos pos = Window.SelectedPos;
				if( !pos.Valid ) {
					Window.AddChat( "no target pos" );
				} else {
					Block block = (Block)Window.Map.GetBlock( pos.BlockPos );
					Window.AddChat( "target pos: " + pos.BlockPos );
					Window.AddChat( "target block: " + Utils.GetSpacedBlockName( block ) );
				}
			} else if( property == "dimensions" ) {
				Window.AddChat( "map width: " + Window.Map.Width );
				Window.AddChat( "map height: " + Window.Map.Height );
				Window.AddChat( "map length: " + Window.Map.Length );
			} else if( property == "jumpheight" ) {
				float jumpHeight = Window.LocalPlayer.JumpHeight;
				Window.AddChat( jumpHeight.ToString( "F2" ) + " blocks" );
			} else {
				Window.AddChat( "&e/client info: Unrecognised property: \"&f" + property + "&e\"." );
			}
		}
	}
	
	public sealed class RenderTypeCommand : Command {
		
		public override string Name {
			get { return "RenderType"; }
		}
		
		public override string[] Help {
			get {
				return new [] {
					"&a/client rendertype [normal/legacy/legacyfast]",
					"&bnormal: &eDefault renderer, with all environmental effects enabled.",
					"&blegacy: &eMay be slightly slower than normal, but produces the same environmental effects.",
					"&blegacyfast: &eSacrifices clouds, fog and overhead sky for faster performance.",
				};
			}
		}
		
		public override void Execute( CommandReader reader ) {
			string property = reader.Next();
			if( property == null ) {
				Window.AddChat( "&e/client rendertype: &cYou didn't specify a new render type." );
			} else if( property == "legacyfast" ) {
				SetNewRenderType( true, true, true );
				Window.AddChat( "&e/client rendertype: &fRender type is now fast legacy." );
			} else if( property == "legacy" ) {
				SetNewRenderType( true, false, true );
				Window.AddChat( "&e/client rendertype: &fRender type is now legacy." );
			} else if( property == "normal" ) {
				SetNewRenderType( false, false, false );
				Window.AddChat( "&e/client rendertype: &fRender type is now normal." );
			}
		}
		
		void SetNewRenderType( bool legacy, bool minimal, bool legacyEnv ) {
			Game game = Window;
			game.MapEnvRenderer.SetUseLegacyMode( legacy );			
			if( minimal ) {
				game.EnvRenderer.Dispose();
				game.EnvRenderer = new MinimalEnvRenderer( game );
				game.EnvRenderer.Init();
			} else {
				if( !( game.EnvRenderer is StandardEnvRenderer ) ) {
					game.EnvRenderer.Dispose();
					game.EnvRenderer = new StandardEnvRenderer( game );
					game.EnvRenderer.Init();
				}
				((StandardEnvRenderer)game.EnvRenderer).SetUseLegacyMode( legacyEnv );
			}
		}
	}
	
	public sealed class ChatFontSizeCommand : Command {
		
		public override string Name {
			get { return "ChatSize"; }
		}
		
		public override string[] Help {
			get {
				return new [] {
					"&a/client chatsize [fontsize]",
					"&bfontsize: &eWhole number specifying the new font size for chat.",
				};
			}
		}
		
		public override void Execute( CommandReader reader ) {
			int fontSize;
			if( !reader.NextInt( out fontSize ) ) {
				Window.AddChat( "&e/client chatsize: &cInvalid font size." );
			} else {
				if( fontSize < 6 ) {
					Window.AddChat( "&e/client chatsize: &cFont size too small." );
					return;
				} else if( fontSize > 30 ) {
					Window.AddChat( "&e/client chatsize: &cFont size too big." );
					return;
				}
				Window.ChatFontSize = fontSize;
				Window.SetNewScreen( null );
				Window.chatInInputBuffer = null;
				Window.SetNewScreen( new NormalScreen( Window ) );
			}
		}
	}
	
	public sealed class MouseSensitivityCommand : Command {
		
		public override string Name {
			get { return "Sensitivity"; }
		}
		
		public override string[] Help {
			get {
				return new [] {
					"&a/client sensitivity [mouse sensitivity]",
					"&bmouse sensitivity: &eInteger between 1 to 100 specifiying the mouse sensitivity.",
				};
			}
		}
		
		public override void Execute( CommandReader reader ) {
			int sensitivity;
			if( !reader.NextInt( out sensitivity ) ) {
				Window.AddChat( "&e/client sensitivity: Current sensitivity is: " + Window.MouseSensitivity );
			} else if( sensitivity < 1 || sensitivity > 100 ) {
				Window.AddChat( "&e/client sensitivity: &cMouse sensitivity must be between 1 to 100." );
			} else {
				Window.MouseSensitivity = sensitivity;
			}
		}
	}
}
