using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using pb = Google.ProtocolBuffers;
using System.Collections.ObjectModel;
namespace ChatClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Session session = null;
        private ObservableCollection<string> my_friends = new ObservableCollection<string>();
        private ObservableCollection<TextMessage> text_messages = new ObservableCollection<TextMessage>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
			ipaddress.Text = "120.24.85.34";
            port.Text = "8888";
            username.Text = "丁小未";
            list_friends.ItemsSource = my_friends;
            list_messages.ItemsSource = text_messages;
        }

        void OnMessage(chat.Message msg)
        {
            if( msg.HasNotification )
            {
                if (msg.Notification.HasWelcome)
                {
                    this.Title = msg.Notification.Welcome.Text.ToStringUtf8();
                }else if( msg.Notification.HasFriend )
                {
                    var name = msg.Notification.Friend.Name.ToStringUtf8();
                    if( msg.Notification.Friend.Online )
                    {
                        my_friends.Add(name);
                    }
                    else
                    {
                        my_friends.Remove(name);
                    }
                }
                else if (msg.Notification.HasMsg)
                {
                    var txtMsg = new TextMessage(msg.Notification.Msg);
                    text_messages.Add(txtMsg);
                }
            }
        }
        void OnConnection(IAsyncResult ar)
        {
          
             Login();
        }
        void Login()
        {
            chat.Message login = new chat.Message.Builder()
            {
                MsgType = chat.MSG.Login_Request,
                Sequence = session.Sequence,
                Request = new chat.Request.Builder()
                {
                    Login = new chat.LoginRequest.Builder()
                    {
                        Username = pb.ByteString.CopyFromUtf8(username.Text)
                    }.Build()
                }.Build()
            }.Build();

            Transaction<chat.Message>.AddRequest(login.Sequence, (chat.Message rsp_msg) => {
                if (rsp_msg.HasResponse && rsp_msg.Response.HasResult && rsp_msg.Response.Result)
                {
                    session.SessionId = rsp_msg.SessionId;
                    this.login.IsEnabled = false;
                    this.logout.IsEnabled = true;
                    GetFriends();

                }
            });
            session.SendMessage(login);
        }
        void GetFriends()
        {
            chat.Message msg = new chat.Message.Builder()
            {
                MsgType = chat.MSG.Get_Friends_Request,
                Sequence = session.Sequence,
                SessionId = session.SessionId,
            
            }.Build();
            Transaction<chat.Message>.AddRequest(msg.Sequence, (chat.Message rsp_msg) =>
            {
                if( rsp_msg.HasResponse && rsp_msg.Response.HasGetFriends )
                {
                    var get_friends = rsp_msg.Response.GetFriends;
                    foreach( var friend in get_friends.FriendsList )
                    {
                        my_friends.Add(friend.Name.ToStringUtf8());
                    }
                }
            });
            session.SendMessage(msg);
        }
        void OnClose(IAsyncResult ar)
        {
            session = null;
        }

        private void login_Click(object sender, RoutedEventArgs e)
        {
            if( session == null )
            {
                session = new Session();
                session.Address = ipaddress.Text;
                session.Port = Int32.Parse(port.Text);
                session.MessageHandler = OnMessage;
                session.OnClose = OnClose;
                session.OnConnection = OnConnection;
                session.Dispatcher = this.Dispatcher;

                session.StartConnection();
            }
            else
            {
                Login();
            }
        }

        private void logout_Click(object sender, RoutedEventArgs e)
        {
            chat.Message msg = new chat.Message.Builder()
            {
                MsgType = chat.MSG.Logout_Request,
                Sequence = session.Sequence,
                SessionId = session.SessionId,

            }.Build();
            Transaction<chat.Message>.AddRequest(msg.Sequence, (chat.Message rsp_msg) =>
            {
                session.SessionId = 0;
                this.login.IsEnabled = true;
                this.logout.IsEnabled = false;
                my_friends.Clear();
                text_messages.Clear();
            });
            session.SendMessage(msg);
        }

        private void send_message_Click(object sender, RoutedEventArgs e)
        {
            if (msg_text.Text.Length == 0)
            {
                return;
            };

            chat.Message msg = new chat.Message.Builder()
            {
                MsgType = chat.MSG.Send_Message_Request,
                Sequence = session.Sequence,
                SessionId = session.SessionId,
                Request = new chat.Request.Builder()
                {
                    SendMessage = new chat.SendMessageRequest.Builder()
                    {
                        Text = pb.ByteString.CopyFromUtf8( msg_text.Text)
                    }.Build()
                }.Build()
            }.Build();

            Transaction<chat.Message>.AddRequest(msg.Sequence, (chat.Message rsp_msg) => {
                if (rsp_msg.Response.Result)
                {
                    //text_messages.Add(new TextMessage(username.Text, msg_text.Text));
                    msg_text.Text = "";
                }
            });
            session.SendMessage(msg);
        }
    }

    public class TextMessage
    {
        public TextMessage( chat.MessageNotification msgNotify)
        {
            Sender = msgNotify.Sender.ToStringUtf8();
            Timestamp = msgNotify.Timestamp;
            Text = msgNotify.Text.ToStringUtf8();
        }
        public TextMessage( string sender, string text)
        {
            Sender = sender;
            Text = text;
            Timestamp = DateTime.Now.ToString();
        }
        public string Sender
        {
            get;
            set;
        }
        public string Timestamp
        {
            get;
            set;
        }
        public string Text
        {
            get;
            set;
        }
    }
}
