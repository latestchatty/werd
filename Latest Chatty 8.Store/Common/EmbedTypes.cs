using System;

namespace Latest_Chatty_8.Common
{
	[Flags]
	public enum EmbedTypes
	{
		None = 0x00,
		Twitter = 0x02,
		Video = 0x04,
		Image = 0x08,
		Youtube = 0x10
	}
}
