using System.Collections.Generic;
using CommandLine;

namespace EnrollMeUcsd
{
	public class CliArguments
	{
		[Option('u', "username", Required = true, 
			HelpText = "The username you use to log into UCSD's WebReg.")]
		public string Username { get; set; }
		
		[Option('p', "password", Required = true,
			HelpText = "The password you use to log into UCSD's WebReg.")]
		public string Password { get; set; }

		[Option('s', "sections", Required = true, 
			HelpText = "The sections that you are trying to enroll in.", Separator = ' ')]
		public IEnumerable<string> SectionIds { get; set; }

		[Option('m', "maxClasses", Default = 1,
			HelpText = "The maximum number of classes that you want this program to enroll in. "
			           + "By default, this is set to '1.'")]
		public int MaxClassesToEnroll { get; set; } 
	}
}