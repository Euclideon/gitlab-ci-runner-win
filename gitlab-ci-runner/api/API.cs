using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack;
using System.Runtime.Serialization;

/// <summary>
/// V1 API
/// </summary>
namespace gitlab_ci_runner.api
{
	[Route ("/runners/register.json","POST")]
	public class RegisterRunner : IReturn<RunnerInfo>
	{
		public string token
		{
			get;
			set;
		}

		public string public_key
		{
			get;
			set;
		}

		public string tag_list
		{
			get;
			set;
		}

		public string description
		{
			get;
			set;
		}
	}

	[Route ("/builds/register.json", "POST")]
	public class CheckForBuild : IReturn<BuildInfo>
	{
		public string token
		{
			get;
			set;
		}
	}

	[Route ("/builds/{id}", "PUT")]
	public class PushBuild : IReturn<string>
	{
		public string id
		{
			get;
			set;
		}

		public string token
		{
			get;
			set;
		}

		public string state
		{
			get;
			set;
		}

		public string trace
		{
			get;
			set;
		}
	}

	public class RunnerInfo
	{
		public int id
		{
			get;
			set;
		}
		public string token
		{
			get;
			set;
		}
	}

	[DataContract]
	public class BuildInfo
	{
		[DataMember]
		public int id
		{
			get;
			set;
		}

		[DataMember]
		public int project_id
		{
			get;
			set;
		}

		[DataMember]
		public string project_name
		{
			get;
			set;
		}

		[DataMember]
		public string commands
		{
			get;
			set;
		}

		[DataMember]
		public string repo_url
		{
			get;
			set;
		}

		[DataMember]
		public string sha
		{
			get;
			set;
		}

		[DataMember]
		public string before_sha
		{
			get;
			set;
		}

		[DataMember(Name = "ref")]
		public string ref_name
		{
			get;
			set;
		}

		[DataMember]
		public int timeout
		{
			get;
			set;
		}

		[DataMember]
		public bool allow_git_fetch
		{
			get;
			set;
		}

		public string[] GetCommands()
		{
			return System.Text.RegularExpressions.Regex.Replace (this.commands, "(\r|\n)+", "\n").Split ('\n');
		}
	}
}
