using System;
using System.Web.Mail;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using www.strive3d.net.Game;

namespace www.strive3d.net.Game
{
	/// <summary>
	/// Summary description for CreateNewPlayer.
	/// </summary>
	public class Player
	{
		public static void Create(string email, string password)
		{
			// insert record:
			CommandFactory c = new CommandFactory();

			SqlCommand createPlayer = c.CreatePlayer(email, password);
			int PlayerID = (int)createPlayer.ExecuteScalar();
			SqlCommand selectPlayer = c.SelectPlayer(PlayerID);
			SqlDataReader playerReader = selectPlayer.ExecuteReader();

			if(playerReader.Read())
			{
				Guid playerKey = (Guid)playerReader["PlayerKey"];
				string signuptext = 
					@"Thanks for your signup.

If you're reading this, it looks like everything worked fine in the signup process.

We've just run this last check to ensure that you are the actual owner of this e-mail address.

To confirm your signup, please click the following link (watch for line wrap)

http://www.strive3d.netGame/activate.aspx?PlayerKey=$PlayerKey$

Regards

The Strive3d team.
";


				MailMessage m = new MailMessage();

				m.From = "system@strive3d.net";
				m.To = email;
				m.Subject = "Signup Confirmation";
				m.Body = signuptext.Replace("$PlayerKey$", playerKey.ToString());
				System.Web.Mail.SmtpMail.SmtpServer = "mail.webhost4life.com";
				System.Web.Mail.SmtpMail.Send(m);
				playerReader.Close();
			}
			else
			{
				playerReader.Close();
			}

			AdminDB admin = new AdminDB();
			admin.AddUserRole(2, PlayerID);

		}

		public static string Activate(Guid playerKey)
		{
			string ActivateReturn = "";
			CommandFactory c = new CommandFactory();
			
			SqlCommand activatePlayer = c.ActivatePlayer(playerKey);
			
			int PlayerID = (int)activatePlayer.ExecuteScalar();

			SqlCommand selectPlayer = c.SelectPlayer(PlayerID);
			SqlDataReader playerReader = selectPlayer.ExecuteReader();

			if(playerReader.Read())
			{
				ActivateReturn = playerReader["Email"].ToString();
				
			}

			playerReader.Close();

			return ActivateReturn;


			
		}
	}
}