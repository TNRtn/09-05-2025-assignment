-- Create Users table
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    ProfileImageUrl NVARCHAR(255),
    Status NVARCHAR(200),
    LastSeen DATETIME,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Create Chats table
CREATE TABLE Chats (
    ChatId INT IDENTITY(1,1) PRIMARY KEY,
    User1Id INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    User2Id INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT UC_UserPair UNIQUE (User1Id, User2Id)
);

-- Create Messages table
CREATE TABLE Messages (
    MessageId INT IDENTITY(1,1) PRIMARY KEY,
    ChatId INT NOT NULL FOREIGN KEY REFERENCES Chats(ChatId),
    SenderId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    Content NVARCHAR(MAX),
    ContentType INT DEFAULT 0, -- 0=Text, 1=Image, 2=Video, etc.
    SentAt DATETIME DEFAULT GETDATE(),
    Status INT DEFAULT 0 -- 0=Sent, 1=Delivered, 2=Read
);

CREATE PROCEDURE UserRegister
    @Email NVARCHAR(255),
    @Name NVARCHAR(100),
    @PasswordHash NVARCHAR(255),
    @UserId INT OUTPUT
AS
BEGIN
   

    IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
    BEGIN
        RAISERROR('Email already registered', 16, 1);
        RETURN;
    END

    INSERT INTO Users (Email, Name, PasswordHash, CreatedAt, LastSeen)
    VALUES (@Email, @Name, @PasswordHash, GETUTCDATE(), GETUTCDATE());

    SET @UserId = SCOPE_IDENTITY();
    SELECT @UserId;
END


DECLARE @NewUserId INT;
EXEC UserRegister 
    @Email = 'user@example.com',
    @Name = 'John Doe',
    @PasswordHash = 'hashed_password_here',
    @UserId = @NewUserId OUTPUT;
SELECT @NewUserId AS NewUserId;

DECLARE @NewUserId INT;
EXEC UserRegister 
    @Email = 'tnr@example.com',
    @Name = 'John Doe',
    @PasswordHash = 'hashed_password_here',
    @UserId = @NewUserId OUTPUT;
SELECT @NewUserId AS NewUserId;

SELECT * FROM Users;

CREATE PROCEDURE UserLogin
    @Email NVARCHAR(255),
    @Password NVARCHAR(255)
AS
BEGIN
   

    SELECT 
        UserId,
        Email,
        Name,
        PasswordHash,
        ProfileImageUrl,
        Status,
        LastSeen
    FROM Users
    WHERE Email = @Email AND PasswordHash = @Password;
END

EXEC UserLogin @Email='user@example.com', @Password='hashed_password_here';

CREATE PROCEDURE UserUpdateLastSeen
    @UserId INT
AS
BEGIN
    

    UPDATE Users
    SET LastSeen = GETUTCDATE()
    WHERE UserId = @UserId;
END

EXEC UserUpdateLastSeen @UserId = 1;

CREATE PROCEDURE ChatGetOrCreate
    @User1Id INT,
    @User2Id INT,
    @ChatId INT OUTPUT
AS
BEGIN
    

    -- Check if chat already exists
    SELECT @ChatId = ChatId
    FROM Chats
    WHERE (User1Id = @User1Id AND User2Id = @User2Id)
       OR (User1Id = @User2Id AND User2Id = @User1Id);

    -- If not exists, create new chat
    IF @ChatId IS NULL
    BEGIN
        INSERT INTO Chats (User1Id, User2Id, CreatedAt)
        VALUES (@User1Id, @User2Id, GETUTCDATE());

        SET @ChatId = SCOPE_IDENTITY();
    END

    SELECT 
        c.ChatId,
        u1.UserId AS User1Id,
        u1.Name AS User1Name,
        u1.Email AS User1Email,
        u1.ProfileImageUrl AS User1ProfileImageUrl,
        u2.UserId AS User2Id,
        u2.Name AS User2Name,
        u2.Email AS User2Email,
        u2.ProfileImageUrl AS User2ProfileImageUrl,
        c.CreatedAt
    FROM Chats c
    JOIN Users u1 ON c.User1Id = u1.UserId
    JOIN Users u2 ON c.User2Id = u2.UserId
    WHERE c.ChatId = @ChatId;
END

DECLARE @ChatId INT;
EXEC ChatGetOrCreate
    @User1Id = 1,
    @User2Id = 2,
    @ChatId = @ChatId OUTPUT;
SELECT @ChatId AS ChatId;

CREATE PROCEDURE ChatGetUserChats
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        c.ChatId,
        CASE 
            WHEN c.User1Id = @UserId THEN u2.UserId
            ELSE u1.UserId
        END AS OtherUserId,
        CASE 
            WHEN c.User1Id = @UserId THEN u2.Name
            ELSE u1.Name
        END AS OtherUserName,
        CASE 
            WHEN c.User1Id = @UserId THEN u2.Email
            ELSE u1.Email
        END AS OtherUserEmail,
        CASE 
            WHEN c.User1Id = @UserId THEN u2.ProfileImageUrl
            ELSE u1.ProfileImageUrl
        END AS OtherUserProfileImageUrl,
        c.CreatedAt,
        m.Content AS LastMessageContent,
        m.SentAt AS LastMessageSentAt,
        m.Status AS LastMessageStatus
    FROM Chats c
    JOIN Users u1 ON c.User1Id = u1.UserId
    JOIN Users u2 ON c.User2Id = u2.UserId
    OUTER APPLY (
        SELECT TOP 1 Content, SentAt, Status
        FROM Messages
        WHERE ChatId = c.ChatId
        ORDER BY SentAt DESC
    ) m
    WHERE c.User1Id = @UserId OR c.User2Id = @UserId
    ORDER BY ISNULL(m.SentAt, c.CreatedAt) DESC;
END

EXEC ChatGetUserChats @UserId = 1;

CREATE PROCEDURE MessageSend
    @ChatId INT,
    @SenderId INT,
    @Content NVARCHAR(MAX),
    @ContentType INT = 0,
    @MessageId INT OUTPUT
AS
BEGIN
    

    -- Verify sender is part of the chat
    IF NOT EXISTS (
        SELECT 1 FROM Chats 
        WHERE ChatId = @ChatId 
        AND (User1Id = @SenderId OR User2Id = @SenderId)
    )
    BEGIN
        RAISERROR('Sender is not part of this chat', 16, 1);
        RETURN;
    END

    -- Insert message
    INSERT INTO Messages (ChatId, SenderId, Content, ContentType, SentAt, Status)
    VALUES (@ChatId, @SenderId, @Content, @ContentType, GETUTCDATE(), 0);

    SET @MessageId = SCOPE_IDENTITY();

    -- Return the full message details
    SELECT 
        m.MessageId,
        m.ChatId,
        m.SenderId,
        u.Name AS SenderName,
        u.Email AS SenderEmail,
        u.ProfileImageUrl AS SenderProfileImageUrl,
        m.Content,
        m.ContentType,
        m.SentAt,
        m.Status
    FROM Messages m
    JOIN Users u ON m.SenderId = u.UserId
    WHERE m.MessageId = @MessageId;
END

DECLARE @MessageId INT;
EXEC MessageSend
    @ChatId = 2,
    @SenderId = 1,
    @Content = 'Hello there!',
    @MessageId = @MessageId OUTPUT;
SELECT @MessageId AS MessageId;


ALTER PROCEDURE MessageGetChatMessages
    @ChatId INT,
    @UserId INT,
    @BeforeDate DATETIME = NULL,
    @Limit INT = 50
AS
BEGIN
    -- Verify user has access to the chat
    IF NOT EXISTS (
        SELECT 1 FROM Chats 
        WHERE ChatId = @ChatId 
        AND (User1Id = @UserId OR User2Id = @UserId)
    )
    BEGIN
        RAISERROR('User is not part of this chat', 16, 1);
        RETURN;
    END

    -- Get messages with proper ordering
    SELECT TOP (@Limit)
        MessageId,
        ChatId,
        SenderId,
        Content,
        ContentType,
        SentAt,
        Status
    FROM Messages
    WHERE ChatId = @ChatId
    AND (@BeforeDate IS NULL OR SentAt < @BeforeDate)
    ORDER BY SentAt DESC;
END

 EXEC MessageGetChatMessages
    @ChatId = 2,
    @UserId = 2,
    @BeforeDate = NULL,
    @Limit = 20;
use assignment;
	select * from Users;
	select * from Chats;
	select * from Messages;

CREATE PROCEDURE MessageMarkAsRead
    @ChatId INT,
    @UserId INT
AS
BEGIN
    -- Update status to 2 (Read) for all messages in this chat
    -- that were sent by others to the current user
    UPDATE Messages
    SET Status = 2
    WHERE ChatId = @ChatId 
      AND SenderId <> @UserId  -- Messages from others
      AND Status < 2           -- Only update if not already read
END
drop proc MessageMarkAsRead

delete from Users
delete from Chats
delete from Messages

CREATE TABLE Employees (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    JoiningDate DATETIME NOT NULL
);

INSERT INTO Employees (FullName, JoiningDate)
VALUES 
('Nageswara Rao', DATEADD(MONTH, -2, GETDATE())),    
('Biswanth', DATEADD(MONTH, -5, GETDATE())),        
('Niteesh', DATEADD(MONTH, -8, GETDATE())),           
('Harshith', DATEADD(MONTH, -1, GETDATE())),         
('Maynikanta', DATEADD(DAY, -20, GETDATE())),        
('Dheeraj', DATEADD(MONTH, -7, GETDATE())),           
('Yashwanth', DATEADD(MONTH, -3, GETDATE())),         
('Iliaz', GETDATE()),                                
('Hemanth', DATEADD(MONTH, -6, GETDATE()));           

-- View all data
SELECT * FROM Employees;

