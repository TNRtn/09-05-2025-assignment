using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace EmployeeChatConsoleApp
{
    class Program
    {
        private static string connectionString = "Server=LAPTOP-2IHUF3HB\\SQLEXPRESS;Database=assignment;Trusted_Connection=True";
        private static User? currentUser = null;

        static async Task Main(string[] args)
        {
            Console.Title = "Employee Chat System";

            var userRepository = new UserRepository(connectionString);
            var chatRepository = new ChatRepository(connectionString);
            var messageRepository = new MessageRepository(connectionString);

            while (true)
            {
                PrintHeader();

                if (currentUser == null)
                {
                    await ShowAuthMenu(userRepository);
                }
                else
                {
                    await ShowMainMenu(chatRepository, messageRepository);
                }
            }
        }

        static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("====================================");
            Console.WriteLine("      EMPLOYEE CHAT SYSTEM");
            Console.WriteLine("====================================");
            if (currentUser != null)
            {
                Console.WriteLine($"Logged in as: {currentUser.Name} ({currentUser.Email})");
            }
            Console.WriteLine("====================================");
            Console.ResetColor();
            Console.WriteLine();
        }

        static async Task ShowAuthMenu(UserRepository userRepository)
        {
            Console.WriteLine("1. Login");
            Console.WriteLine("2. Register");
            Console.WriteLine("3. Exit");
            Console.Write("\nSelect an option (1-3): ");

            var option = Console.ReadLine();

            try
            {
                switch (option)
                {
                    case "1":
                        currentUser = await Login(userRepository);
                        break;
                    case "2":
                        currentUser = await RegisterUser(userRepository);
                        break;
                    case "3":
                        Environment.Exit(0);
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\nInvalid option. Please try again.");
                        Console.ResetColor();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError: {ex.Message}");
                Console.ResetColor();
            }
        }

        static async Task ShowMainMenu(ChatRepository chatRepository, MessageRepository messageRepository)
        {
            Console.WriteLine("1. Start New Chat");
            Console.WriteLine("2. View My Chats");
            Console.WriteLine("3. Send Message");
            Console.WriteLine("4. View Messages");
            Console.WriteLine("5. Logout");
            Console.Write("\nSelect an option (1-5): ");

            var option = Console.ReadLine();

            try
            {
                switch (option)
                {
                    case "1":
                        await StartChat(chatRepository);
                        break;
                    case "2":
                        await ViewChats(chatRepository);
                        break;
                    case "3":
                        await SendMessage(messageRepository);
                        break;
                    case "4":
                        await ViewMessages(messageRepository);
                        break;
                    case "5":
                        currentUser = null;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\nYou have been logged out.");
                        Console.ResetColor();
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\nInvalid option. Please try again.");
                        Console.ResetColor();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError: {ex.Message}");
                Console.ResetColor();
            }
        }

        static async Task<User?> RegisterUser(UserRepository userRepository)
        {
            PrintHeader();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--- USER REGISTRATION ---");
            Console.ResetColor();

            Console.Write("Email: ");
            var email = Console.ReadLine();
            Console.Write("Name: ");
            var name = Console.ReadLine();
            Console.Write("Password: ");
            var password = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nAll fields are required!");
                Console.ResetColor();
                return null;
            }

            try
            {
                var userId = await userRepository.RegisterUserAsync(email, name, password);
                await userRepository.UpdateLastSeenAsync(userId);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nRegistration successful!");
                Console.ResetColor();

                return new User
                {
                    UserId = userId,
                    Email = email,
                    Name = name,
                    PasswordHash = password,
                    LastSeen = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nRegistration failed: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }

        static async Task<User?> Login(UserRepository userRepository)
        {
            PrintHeader();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--- USER LOGIN ---");
            Console.ResetColor();

            Console.Write("Email: ");
            var email = Console.ReadLine();
            Console.Write("Password: ");
            var password = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nBoth email and password are required!");
                Console.ResetColor();
                return null;
            }

            try
            {
                var user = await userRepository.LoginAsync(email, password);
                if (user != null)
                {
                    await userRepository.UpdateLastSeenAsync(user.UserId);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nLogin successful!");
                    Console.ResetColor();
                    return user;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nInvalid email or password!");
                    Console.ResetColor();
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nLogin failed: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }

        static async Task StartChat(ChatRepository chatRepository)
        {
            PrintHeader();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--- START NEW CHAT ---");
            Console.ResetColor();

            try
            {
                var availableUsers = await chatRepository.GetAvailableUsersAsync(currentUser!.UserId);

                if (availableUsers.Count == 0)
                {
                    Console.WriteLine("\nNo other users available to chat with.");
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("\nAvailable users to chat with:");
                foreach (var user in availableUsers)
                {
                    Console.WriteLine($"ID: {user.UserId} - {user.Name} ({user.Email})");
                }

                Console.Write("\nEnter the ID of the user you want to chat with: ");
                if (!int.TryParse(Console.ReadLine(), out int user2Id))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nInvalid user ID!");
                    Console.ResetColor();
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    return;
                }

                var chat = await chatRepository.GetOrCreateChatAsync(currentUser.UserId, user2Id);
                if (chat != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\nChat created successfully! Chat ID: {chat.ChatId}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nFailed to create chat. The other user may not exist.");
                    Console.ResetColor();
                }
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError creating chat: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        static async Task ViewChats(ChatRepository chatRepository)
        {
            PrintHeader();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--- YOUR CHATS ---");
            Console.ResetColor();

            try
            {
                var chats = await chatRepository.GetUserChatsAsync(currentUser!.UserId);

                if (chats.Count == 0)
                {
                    Console.WriteLine("\nYou don't have any chats yet.");
                }
                else
                {
                    Console.WriteLine($"\n{"Chat ID",-8} {"With",-20} {"Last Message",-30} {"Time"}");
                    Console.WriteLine(new string('-', 70));

                    foreach (var chat in chats)
                    {
                        Console.Write($"{chat.ChatId,-8} {chat.OtherUser.Name,-20} ");
                        if (chat.LastMessage != null)
                        {
                            var content = chat.LastMessage.Content.Length > 25 ?
                                chat.LastMessage.Content.Substring(0, 22) + "..." :
                                chat.LastMessage.Content;
                            Console.WriteLine($"{content,-30} {chat.LastMessage.SentAt:g}");
                        }
                        else
                        {
                            Console.WriteLine("No messages yet");
                        }
                    }
                }
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError retrieving chats: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        static async Task SendMessage(MessageRepository messageRepository)
        {
            PrintHeader();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--- SEND MESSAGE ---");
            Console.ResetColor();

            try
            {
                var chats = await messageRepository.GetUserChatsAsync(currentUser!.UserId);

                if (chats.Count == 0)
                {
                    Console.WriteLine("\nYou don't have any chats yet. Start a new chat first.");
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("\nYour available chats:");
                foreach (var chat in chats)
                {
                    Console.WriteLine($"Chat ID: {chat.ChatId} - With: {chat.OtherUser.Name}");
                }

                Console.Write("\nEnter Chat ID: ");
                if (!int.TryParse(Console.ReadLine(), out int chatId))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nInvalid Chat ID!");
                    Console.ResetColor();
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    return;
                }

                Console.Write("\nEnter your message: ");
                string content = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(content))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nMessage cannot be empty!");
                    Console.ResetColor();
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    return;
                }

                var message = await messageRepository.SendMessageAsync(chatId, currentUser.UserId, content);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nMessage sent successfully at {message.SentAt:g}");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
            catch (UnauthorizedAccessException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nYou are not part of this chat!");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nFailed to send message: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        static async Task ViewMessages(MessageRepository messageRepository)
        {
            PrintHeader();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--- VIEW MESSAGES ---");
            Console.ResetColor();

            try
            {
                var chats = await messageRepository.GetUserChatsAsync(currentUser!.UserId);

                if (chats.Count == 0)
                {
                    Console.WriteLine("\nYou don't have any chats yet. Start a new chat first.");
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("\nYour available chats:");
                foreach (var chat in chats)
                {
                    Console.WriteLine($"Chat ID: {chat.ChatId} - With: {chat.OtherUser.Name}");
                }

                Console.Write("\nEnter Chat ID: ");
                if (!int.TryParse(Console.ReadLine(), out int chatId))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nInvalid Chat ID!");
                    Console.ResetColor();
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    return;
                }

                // Get the specific chat details
                var selectedChat = chats.FirstOrDefault(c => c.ChatId == chatId);
                if (selectedChat == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nChat not found!");
                    Console.ResetColor();
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    return;
                }

                // Display chat information
                Console.WriteLine("\n----------------------------------------");
                Console.WriteLine($"Chat with: {selectedChat.OtherUser.Name}");
                Console.WriteLine($"Started on: {selectedChat.CreatedAt:g}");
                if (selectedChat.LastMessage != null)
                {
                    Console.WriteLine($"Last message: {selectedChat.LastMessage.SentAt:g}");
                }
                Console.WriteLine("----------------------------------------\n");

                var messages = await messageRepository.GetChatMessagesAsync(chatId, currentUser.UserId);

                Console.WriteLine($"\n{"Time",-20} {"Sender",-15} {"Message"}");
                Console.WriteLine(new string('-', 70));

                if (messages.Count == 0)
                {
                    Console.WriteLine("No messages in this chat yet.");
                }
                else
                {
                    foreach (var msg in messages)
                    {
                        var senderName = msg.SenderId == currentUser.UserId ? "You" : selectedChat.OtherUser.Name;
                        Console.WriteLine($"{msg.SentAt:g,-20} {senderName,-15} {msg.Content}");
                    }
                }

                await messageRepository.MarkMessagesAsReadAsync(chatId, currentUser.UserId);
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
            catch (UnauthorizedAccessException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nYou are not part of this chat!");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nFailed to retrieve messages: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

    }

    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string? Status { get; set; }
        public DateTime LastSeen { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Chat
    {
        public int ChatId { get; set; }
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ChatDetail
    {
        public int ChatId { get; set; }
        public User OtherUser { get; set; } = new User();
        public DateTime CreatedAt { get; set; }
        public Message? LastMessage { get; set; }
    }

    public class Message
    {
        public int MessageId { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int ContentType { get; set; }
        public DateTime SentAt { get; set; }
        public int Status { get; set; }
    }

    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> RegisterUserAsync(string email, string name, string passwordHash)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UserRegister", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Email", email);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@PasswordHash", passwordHash);

            var userIdParam = new SqlParameter("@UserId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(userIdParam);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            if (userIdParam.Value == DBNull.Value)
                throw new Exception("User registration failed");

            return (int)userIdParam.Value;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("UserLogin", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Password", password);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    UserId = reader.GetInt32(0),
                    Email = reader.GetString(1),
                    Name = reader.GetString(2),
                    PasswordHash = reader.GetString(3),
                    ProfileImageUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Status = reader.IsDBNull(5) ? null : reader.GetString(5),
                    LastSeen = reader.GetDateTime(6)
                };
            }
            return null;
        }

        public async Task UpdateLastSeenAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("UserUpdateLastSeen", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId", userId);
            await connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public class ChatRepository
    {
        private readonly string _connectionString;

        public ChatRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<User>> GetAvailableUsersAsync(int currentUserId)
        {
            var users = new List<User>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(
                "SELECT UserId, Name, Email FROM Users WHERE UserId != @CurrentUserId",
                connection);

            command.Parameters.AddWithValue("@CurrentUserId", currentUserId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2)
                });
            }

            return users;
        }

        public async Task<Chat?> GetOrCreateChatAsync(int user1Id, int user2Id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("ChatGetOrCreate", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@User1Id", user1Id);
            cmd.Parameters.AddWithValue("@User2Id", user2Id);
            var chatIdParam = new SqlParameter("@ChatId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(chatIdParam);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            if (chatIdParam.Value == DBNull.Value)
                return null;

            return new Chat
            {
                ChatId = (int)chatIdParam.Value,
                User1Id = user1Id,
                User2Id = user2Id,
                CreatedAt = DateTime.Now
            };
        }

        public async Task<List<ChatDetail>> GetUserChatsAsync(int userId)
        {
            var chats = new List<ChatDetail>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("ChatGetUserChats", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                chats.Add(new ChatDetail
                {
                    ChatId = reader.GetInt32(0),
                    OtherUser = new User
                    {
                        UserId = reader.GetInt32(1),
                        Name = reader.GetString(2),
                        Email = reader.GetString(3),
                        ProfileImageUrl = reader.IsDBNull(4) ? null : reader.GetString(4)
                    },
                    CreatedAt = reader.GetDateTime(5),
                    LastMessage = reader.IsDBNull(6) ? null : new Message
                    {
                        Content = reader.GetString(6),
                        SentAt = reader.GetDateTime(7),
                        Status = reader.GetInt32(8)
                    }
                });
            }

            return chats;
        }
    }

    public class MessageRepository
    {
        private readonly string _connectionString;

        public MessageRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<ChatDetail>> GetUserChatsAsync(int userId)
        {
            var chats = new List<ChatDetail>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("ChatGetUserChats", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                chats.Add(new ChatDetail
                {
                    ChatId = reader.GetInt32(0),
                    OtherUser = new User
                    {
                        UserId = reader.GetInt32(1),
                        Name = reader.GetString(2),
                        Email = reader.GetString(3),
                        ProfileImageUrl = reader.IsDBNull(4) ? null : reader.GetString(4)
                    },
                    CreatedAt = reader.GetDateTime(5),
                    LastMessage = reader.IsDBNull(6) ? null : new Message
                    {
                        Content = reader.GetString(6),
                        SentAt = reader.GetDateTime(7),
                        Status = reader.GetInt32(8)
                    }
                });
            }

            return chats;
        }

        public async Task<Message> SendMessageAsync(int chatId, int senderId, string content, int contentType = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Verify sender is part of the chat
            using var verifyCommand = new SqlCommand(
                "SELECT 1 FROM Chats WHERE ChatId = @ChatId AND (User1Id = @SenderId OR User2Id = @SenderId)",
                connection);

            verifyCommand.Parameters.AddWithValue("@ChatId", chatId);
            verifyCommand.Parameters.AddWithValue("@SenderId", senderId);

            if (await verifyCommand.ExecuteScalarAsync() == null)
            {
                throw new UnauthorizedAccessException("Sender is not part of this chat");
            }

            // Insert the message
            using var insertCommand = new SqlCommand(
                "INSERT INTO Messages (ChatId, SenderId, Content, ContentType, SentAt, Status) " +
                "VALUES (@ChatId, @SenderId, @Content, @ContentType, GETUTCDATE(), 0); " +
                "SELECT SCOPE_IDENTITY();",
                connection);

            insertCommand.Parameters.AddWithValue("@ChatId", chatId);
            insertCommand.Parameters.AddWithValue("@SenderId", senderId);
            insertCommand.Parameters.AddWithValue("@Content", content);
            insertCommand.Parameters.AddWithValue("@ContentType", contentType);

            var messageId = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());

            // Return message details
            using var selectCommand = new SqlCommand(
                "SELECT MessageId, ChatId, SenderId, Content, ContentType, SentAt, Status " +
                "FROM Messages WHERE MessageId = @MessageId",
                connection);

            selectCommand.Parameters.AddWithValue("@MessageId", messageId);

            using var reader = await selectCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Message
                {
                    MessageId = reader.GetInt32(0),
                    ChatId = reader.GetInt32(1),
                    SenderId = reader.GetInt32(2),
                    Content = reader.GetString(3),
                    ContentType = reader.GetInt32(4),
                    SentAt = reader.GetDateTime(5),
                    Status = reader.GetInt32(6)
                };
            }

            throw new Exception("Failed to retrieve sent message");
        }

        public async Task<List<Message>> GetChatMessagesAsync(int chatId, int userId, DateTime? beforeDate = null, int limit = 50)
        {
            var messages = new List<Message>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Verify user is part of the chat
            using var verifyCommand = new SqlCommand(
                "SELECT 1 FROM Chats WHERE ChatId = @ChatId AND (User1Id = @UserId OR User2Id = @UserId)",
                connection);

            verifyCommand.Parameters.AddWithValue("@ChatId", chatId);
            verifyCommand.Parameters.AddWithValue("@UserId", userId);

            if (await verifyCommand.ExecuteScalarAsync() == null)
            {
                throw new UnauthorizedAccessException("User is not part of this chat");
            }

            // Get messages
            using var messageCommand = new SqlCommand(
                "SELECT MessageId, ChatId, SenderId, Content, ContentType, SentAt, Status " +
                "FROM Messages WHERE ChatId = @ChatId " +
                "ORDER BY SentAt DESC",
                connection);

            messageCommand.Parameters.AddWithValue("@ChatId", chatId);

            using var reader = await messageCommand.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                messages.Add(new Message
                {
                    MessageId = reader.GetInt32(0),
                    ChatId = reader.GetInt32(1),
                    SenderId = reader.GetInt32(2),
                    Content = reader.GetString(3),
                    ContentType = reader.GetInt32(4),
                    SentAt = reader.GetDateTime(5),
                    Status = reader.GetInt32(6)
                });
            }

            return messages;
        }

        public async Task<int> MarkMessagesAsReadAsync(int chatId, int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(
                "UPDATE Messages SET Status = 1 " +
                "WHERE ChatId = @ChatId AND SenderId != @UserId AND Status = 0",
                connection);

            command.Parameters.AddWithValue("@ChatId", chatId);
            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }
    }
}