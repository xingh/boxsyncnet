﻿using System;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading;

using BoxSync.Core.Primitives;
using BoxSync.Core.ServiceReference;
using BoxSync.Core.Statuses;

using ICSharpCode.SharpZipLib.Zip;


namespace BoxSync.Core
{
	/// <summary>
	/// Provides methods for using Box.NET SOAP web service
	/// </summary>
	public sealed class BoxManager
	{
		private readonly boxnetService _service;
		private readonly string _apiKey;
		private string _token;
		private readonly IWebProxy _proxy;
		private TagPrimitiveCollection _tagCollection;
		

		/// <summary>
		/// Instantiates BoxManager
		/// </summary>
		/// <param name="applicationApiKey">The unique API key which is assigned to application</param>
		/// <param name="serviceUrl">Box.NET SOAP service Url</param>
		/// <param name="proxy">Proxy information</param>
		public BoxManager(string applicationApiKey, string serviceUrl, IWebProxy proxy) :
			this(applicationApiKey, serviceUrl, proxy, null)
		{
		}

		/// <summary>
		/// Instantiates BoxManager
		/// </summary>
		/// <param name="applicationApiKey">The unique API key which is assigned to application</param>
		/// <param name="serviceUrl">Box.NET SOAP service Url</param>
		/// <param name="proxy">Proxy information</param>
		/// <param name="authorizationTocken">Valid authorization tocken</param>
		public BoxManager(string applicationApiKey, string serviceUrl, IWebProxy proxy, string authorizationTocken)
		{
			_apiKey = applicationApiKey;
			
			_service = new boxnetService();
			_proxy = proxy;
			
			_service.Url = serviceUrl;
			_service.Proxy = proxy;

			_token = authorizationTocken;
		}


		/// <summary>
		/// Gets or sets authentication token required for communication 
		/// between Box.NET service and user's application
		/// </summary>
		public string AuthenticationToken
		{
			get
			{
				return _token;
			}
			set
			{
				_token = value;
			}
		}

		/// <summary>
		/// Proxy used to access Box.NET service
		/// </summary>
		public IWebProxy Proxy
		{
			get
			{
				return _proxy;
			}
		}


		#region AuthenticateUser

		/// <summary>
		/// Authenticates user
		/// </summary>
		/// <param name="login">Account login</param>
		/// <param name="password">Account password</param>
		/// <param name="method"></param>
		/// <param name="authenticationToken">Authentication token</param>
		/// <param name="authenticatedUser">Authenticated user information</param>
		/// <returns>Operation result</returns>
		public AuthorizeStatus AuthenticateUser(string login, string password, string method, out string authenticationToken, out User authenticatedUser)
		{
			SOAPUser user;
			
			string result = _service.authorization(_apiKey, login, password, method, out authenticationToken, out user);
			
			authenticatedUser = new User(user);

			return StatusMessageParser.ParseAuthorizeStatus(result);
		}

		/// <summary>
		/// Authenticates user
		/// </summary>
		/// <param name="login">Account login</param>
		/// <param name="password">Account password</param>
		/// <param name="method"></param>
		/// <param name="authenticateUserCompleted">Callback method which will be invoked when operation completes</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="authenticateUserCompleted"/> is null</exception>
		public void AuthenticateUser(
			string login,
			string password,
			string method,
			OperationFinished<AuthenticateUserResponse> authenticateUserCompleted)
		{
			AuthenticateUser(login, password, method, authenticateUserCompleted, null);
		}

		/// <summary>
		/// Authenticates user
		/// </summary>
		/// <param name="login">Account login</param>
		/// <param name="password">Account password</param>
		/// <param name="method"></param>
		/// <param name="authenticateUserCompleted">Callback method which will be invoked when operation completes</param>
		/// <param name="userState"></param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="authenticateUserCompleted"/> is null</exception>
		public void AuthenticateUser(
			string login,
			string password,
			string method,
			OperationFinished<AuthenticateUserResponse> authenticateUserCompleted,
			object userState)
		{
			ThrowIfParameterIsNull(authenticateUserCompleted, "authenticateUserCompleted");

			_service.authorizationCompleted += AuthorizationFinished;

			object[] state = {authenticateUserCompleted, userState};

			_service.authorizationAsync(_apiKey, login, password, method, state);
		}

		private void AuthorizationFinished(object sender, authorizationCompletedEventArgs e)
		{
			object[] state = (object[])e.UserState;

			OperationFinished<AuthenticateUserResponse> authenticateUserCompleted =
				(OperationFinished<AuthenticateUserResponse>)state[0];

			AuthorizeStatus status = StatusMessageParser.ParseAuthorizeStatus(e.Result);
			AuthenticateUserResponse response;

			switch (status)
			{
				case AuthorizeStatus.Successful:
					User authenticatedUser = new User(e.user);
					response = new AuthenticateUserResponse
					           	{
					           		AuthenticatedUser = authenticatedUser,
					           		Status = status,
					           		Token = e.auth_token,
					           		UserState = state[1]
					           	};
					authenticateUserCompleted(response, null);
					break;
				case AuthorizeStatus.Failed:
					response = new AuthenticateUserResponse
					           	{
									Status = status,
					           		UserState = state[1]
					           	};
					authenticateUserCompleted(response, null);
					break;
				default:
					authenticateUserCompleted(null, e.Result);
					break;
			}
		}

		#endregion

		#region GetAuthenticationToken

		/// <summary>
		/// Gets authentication token required for communication between Box.NET service and user's application.
		/// Method habe to be called after the user has authorized themself on Box.NET site
		/// </summary>
		/// <param name="authenticationTicket">Athentication ticket</param>
		/// <param name="authenticationToken">Authentication token</param>
		/// <param name="authenticatedUser">Authenticated user account information</param>
		/// <returns>Operation result</returns>
		public GetAuthenticationTokenStatus GetAuthenticationToken(string authenticationTicket, out string authenticationToken, out User authenticatedUser)
		{
			SOAPUser user;

			string result = _service.get_auth_token(_apiKey, authenticationTicket, out authenticationToken, out user);

			authenticatedUser = new User(user);

			return StatusMessageParser.ParseGetAuthenticationTockenStatus(result);
		}

		/// <summary>
		/// Gets authentication token required for communication between Box.NET service and user's application.
		/// Method habe to be called after the user has authorized themself on Box.NET site
		/// </summary>
		/// <param name="authenticationTicket">Athentication ticket</param>
		/// <param name="getAuthenticationTokenCompleted">Call back method which will be invoked when operation completes</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="getAuthenticationTokenCompleted"/> is null</exception>
		public void GetAuthenticationToken(string authenticationTicket, OperationFinished<GetAuthenticationTokenResponse> getAuthenticationTokenCompleted)
		{
			GetAuthenticationToken(authenticationTicket, getAuthenticationTokenCompleted, null);
		}

		/// <summary>
		/// Gets authentication token required for communication between Box.NET service and user's application.
		/// Method habe to be called after the user has authorized themself on Box.NET site
		/// </summary>
		/// <param name="authenticationTicket">Athentication ticket</param>
		/// <param name="getAuthenticationTokenCompleted">Call back method which will be invoked when operation completes</param>
		/// <param name="userState"></param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="getAuthenticationTokenCompleted"/> is null</exception>
		public void GetAuthenticationToken(string authenticationTicket, OperationFinished<GetAuthenticationTokenResponse> getAuthenticationTokenCompleted, object userState)
		{
			ThrowIfParameterIsNull(getAuthenticationTokenCompleted, "getAuthenticationTokenCompleted");

			_service.get_auth_tokenCompleted += GetAuthenticationTokenFinished;

			object[] state = {getAuthenticationTokenCompleted, userState};

			_service.get_auth_tokenAsync(_apiKey, authenticationTicket, state);
		}

		private void GetAuthenticationTokenFinished(object sender, get_auth_tokenCompletedEventArgs e)
		{
			object[] state = (object[])e.UserState;

			OperationFinished<GetAuthenticationTokenResponse> getAuthenticationTokenCompleted =
				(OperationFinished<GetAuthenticationTokenResponse>)state[0];

			GetAuthenticationTokenStatus status = StatusMessageParser.ParseGetAuthenticationTockenStatus(e.Result);

			GetAuthenticationTokenResponse response = new GetAuthenticationTokenResponse
			{	
				Status = status,
				UserState = state[1]
			};


			switch (status)
			{
				case GetAuthenticationTokenStatus.Successful:
					User authenticatedUser = new User(e.user);
					response.AuthenticatedUser = authenticatedUser;
					response.AuthenticationToken = e.auth_token;

					getAuthenticationTokenCompleted(response, null);
					break;
				case GetAuthenticationTokenStatus.Failed:
					getAuthenticationTokenCompleted(response, null);
					break;
				default:
					getAuthenticationTokenCompleted(response, e.Result);
					break;
			}
		}

		#endregion

		#region GetTicket

		/// <summary>
		/// Gets ticket which is used to generate an authentication page 
		/// for the user to login
		/// </summary>
		/// <param name="authenticationTicket">Authentication ticket</param>
		/// <returns>Operation status</returns>
		public GetTicketStatus GetTicket(out string authenticationTicket)
		{
			string result = _service.get_ticket(_apiKey, out authenticationTicket);

			return StatusMessageParser.ParseGetTicketStatus(result);
		}

		/// <summary>
		/// Gets ticket which is used to generate an authentication page 
		/// for the user to login
		/// </summary>
		/// <param name="getAuthenticationTicketCompleted">Call back method which will be invoked when operation completes</param>
		public void GetTicket(OperationFinished<GetTicketResponse> getAuthenticationTicketCompleted)
		{
			GetTicket(getAuthenticationTicketCompleted, null);
		}

		/// <summary>
		/// Gets ticket which is used to generate an authentication page 
		/// for the user to login
		/// </summary>
		/// <param name="getAuthenticationTicketCompleted">Call back method which will be invoked when operation completes</param>
		/// <param name="userState"></param>
		public void GetTicket(OperationFinished<GetTicketResponse> getAuthenticationTicketCompleted, object userState)
		{
			ThrowIfParameterIsNull(getAuthenticationTicketCompleted, "getAuthenticationTicketCompleted");

			object[] data = {getAuthenticationTicketCompleted, userState};

			_service.get_ticketCompleted += GetTicketFinished;

			_service.get_ticketAsync(_apiKey, data);
		}

		private void GetTicketFinished(object sender, get_ticketCompletedEventArgs e)
		{
			object[] data = (object[]) e.UserState;
			OperationFinished<GetTicketResponse> getAuthenticationTicketCompleted = (OperationFinished<GetTicketResponse>)data[0];
			GetTicketStatus status = StatusMessageParser.ParseGetTicketStatus(e.Result);
			GetTicketResponse response = new GetTicketResponse { Status = status, Ticket = e.ticket, data[1] };

			switch (status)
			{
				case GetTicketStatus.Successful:
					getAuthenticationTicketCompleted(response, null);
					break;
				default:
					getAuthenticationTicketCompleted(response, e.Result);
					break;
			}
		}

		#endregion

		#region Upload file

		/// <summary>
		/// Adds the specified local file to the specified folder
		/// </summary>
		/// <param name="filePath">Path to the file which needs to be uploaded</param>
		/// <param name="destinationFolderID">ID of the destination folder</param>
		/// <returns>Operation status</returns>
		public UploadFileResponse AddFile(string filePath, long destinationFolderID)
		{
			UploadFileResponse uploadFileResponse;
			
			using (WebClient client = new WebClient { Proxy = Proxy })
			{
				Uri destinationAddress = new Uri(string.Format("http://upload.box.net/api/1.0/upload/{0}/{1}", AuthenticationToken, destinationFolderID));

				byte[] response = client.UploadFile(destinationAddress, "POST", filePath);
				
				string result = Encoding.ASCII.GetString(response);

				uploadFileResponse = MessageParser.Instance.ParseUploadResponseMessage(result);
				uploadFileResponse.FolderID = destinationFolderID;
			}

			return uploadFileResponse;
		}

		/// <summary>
		/// Asynchronously adds the specified local file to the specified folder
		/// </summary>
		/// <param name="filePath">Path to the file which needs to be uploaded</param>
		/// <param name="parentFolderID">ID of the destination folder</param>
		/// <param name="fileUploadCompleted">Callback method which will be invoked after file-upload operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="fileUploadCompleted"/> is null</exception>
		public void AddFile(string filePath, long parentFolderID, OperationFinished<UploadFileResponse> fileUploadCompleted)
		{
			AddFile(filePath, parentFolderID, fileUploadCompleted, null);
		}

		/// <summary>
		/// Asynchronously adds the specified local file to the specified folder
		/// </summary>
		/// <param name="filePath">Path to the file which needs to be uploaded</param>
		/// <param name="parentFolderID">ID of the destination folder</param>
		/// <param name="fileUploadCompleted">Callback method which will be invoked after file-upload operation completes</param>
		/// <param name="userState"></param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="fileUploadCompleted"/> is null</exception>
		public void AddFile(string filePath, long parentFolderID, OperationFinished<UploadFileResponse> fileUploadCompleted, object userState)
		{
			ThrowIfParameterIsNull(fileUploadCompleted, "fileUploadCompleted");

			using (WebClient client = new WebClient { Proxy = Proxy })
			{
				Uri destinationAddress = new Uri(string.Format("http://upload.box.net/api/1.0/upload/{0}/{1}", AuthenticationToken, parentFolderID));

				client.UploadFileCompleted += UploadFileFinished;

				object[] state = new object[3];

				state[0] = fileUploadCompleted;
				state[1] = userState;
				state[2] = parentFolderID;

				client.UploadFileAsync(destinationAddress, "POST", filePath, state);
			}
		}
		
		/// <summary>
		/// Handler method which will be executed after file-upload operation completes
		/// </summary>
		/// <param name="sender">The source of the event</param>
		/// <param name="e">Argument that contains event data</param>
		private void UploadFileFinished(object sender, UploadFileCompletedEventArgs e)
		{
			object[] state = (object[])e.UserState;
			long folderID = (long)state[2];
			object userState = state[1];
			OperationFinished<UploadFileResponse> fileUploadFinishedHandler = (OperationFinished<UploadFileResponse>)state[0];

			string result = Encoding.ASCII.GetString(e.Result);

			UploadFileResponse uploadFileResponse = MessageParser.Instance.ParseUploadResponseMessage(result);

			uploadFileResponse.FolderID = folderID;
			uploadFileResponse.UserState = userState;

			switch(uploadFileResponse.Status)
			{
				case UploadFileStatus.Successful:
				case UploadFileStatus.ApplicationRestricted:
				case UploadFileStatus.Failed:
				case UploadFileStatus.NotLoggedID:
					fileUploadFinishedHandler(uploadFileResponse, null);
					break;
				default:
					fileUploadFinishedHandler(uploadFileResponse, e.Result);
					break;
			}
		}
		#endregion

		#region Create folder
		
		/// <summary>
		/// Creates folder
		/// </summary>
		/// <param name="folderName">Folder name</param>
		/// <param name="parentFolderID">ID of the parent folder where new folder needs to be created or '0'</param>
		/// <param name="isShared">Indicates if new folder will be publicly shared</param>
		/// <param name="folder">Contains all information about newly created folder</param>
		/// <returns>Operation status</returns>
		public CreateFolderStatus CreateFolder(string folderName, long parentFolderID, bool isShared, out FolderBase folder)
		{
			SOAPFolder soapFolder;
			string response = _service.create_folder(_apiKey, AuthenticationToken, parentFolderID, folderName, isShared ? 1 : 0, out soapFolder);
			
			folder = new FolderBase(soapFolder);

			return StatusMessageParser.ParseAddFolderStatus(response);
		}

		/// <summary>
		/// Asynchronously creates folder
		/// </summary>
		/// <param name="folderName">Folder name</param>
		/// <param name="parentFolderID">ID of the parent folder where new folder needs to be created or '0'</param>
		/// <param name="isShared">Indicates if new folder will be publicly shared</param>
		/// <param name="createFolderCompleted">Callback method which will be invoked after operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="createFolderCompleted"/> is null</exception>
		public void CreateFolder(string folderName, long parentFolderID, bool isShared, OperationFinished<CreateFolderResponse> createFolderCompleted)
		{
			CreateFolder(folderName, parentFolderID, isShared, createFolderCompleted, null);
		}

		/// <summary>
		/// Asynchronously creates folder
		/// </summary>
		/// <param name="folderName">Folder name</param>
		/// <param name="parentFolderID">ID of the parent folder where new folder needs to be created or '0'</param>
		/// <param name="isShared">Indicates if new folder will be publicly shared</param>
		/// <param name="createFolderCompleted">Callback method which will be invoked after operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="createFolderCompleted"/> is null</exception>
		public void CreateFolder(string folderName, long parentFolderID, bool isShared, OperationFinished<CreateFolderResponse> createFolderCompleted, object userState)
		{
			ThrowIfParameterIsNull(createFolderCompleted, "createFolderCompleted");

			_service.create_folderCompleted += CreateFolderFinished;

			object[] state = {createFolderCompleted, userState};

			_service.create_folderAsync(_apiKey, AuthenticationToken, parentFolderID, folderName, isShared ? 1 : 0,
										state);
		}

		private void CreateFolderFinished(object sender, create_folderCompletedEventArgs e)
		{
			object[] state = (object[]) e.UserState;
			OperationFinished<CreateFolderResponse> createFolderFinishedHandler =
				(OperationFinished<CreateFolderResponse>)state[0];

			CreateFolderStatus status = StatusMessageParser.ParseAddFolderStatus(e.Result);

			CreateFolderResponse response = new CreateFolderResponse
			                                	{
			                                		Status = status,
			                                		UserState = state[1]
			                                	};

			switch (status)
			{
				case CreateFolderStatus.Successful:
				case CreateFolderStatus.ApplicationRestricted:
				case CreateFolderStatus.NoParentFolder:
				case CreateFolderStatus.NotLoggedIn:
					response.Folder = new FolderBase(e.folder);

					createFolderFinishedHandler(response, null);
					break;
				default:
					createFolderFinishedHandler(response, e.Result);
					break;
			}
		}
		
		#endregion

		#region Delete object

		/// <summary>
		/// Deletes specified object
		/// </summary>
		/// <param name="objectID">ID of the object to delete</param>
		/// <param name="objectType">Type of the object</param>
		/// <returns>Operation status</returns>
		public DeleteObjectStatus DeleteObject(long objectID, ObjectType objectType)
		{
			string type = ObjectType2String(objectType);
			string result = _service.delete(_apiKey, AuthenticationToken, type, objectID);

			return StatusMessageParser.ParseDeleteObjectStatus(result);
		}
		
		/// <summary>
		/// Asynchronously deletes specified object
		/// </summary>
		/// <param name="objectID">ID of the object to delete</param>
		/// <param name="objectType">Type of the object</param>
		/// <param name="deleteObjectCompleted">Callback method which will be invoked after delete operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="deleteObjectCompleted"/> is null</exception>
		public void DeleteObject(long objectID, ObjectType objectType, OperationFinished<DeleteObjectStatus> deleteObjectCompleted)
		{
			ThrowIfParameterIsNull(deleteObjectCompleted, "deleteObjectCompleted");

			string type = ObjectType2String(objectType);

			_service.deleteCompleted += DeleteObjectFinished;

			_service.deleteAsync(_apiKey, AuthenticationToken, type, objectID, deleteObjectCompleted);
		}

		/// <summary>
		/// Asynchronously deletes specified object
		/// </summary>
		/// <param name="objectID">ID of the object to delete</param>
		/// <param name="objectType">Type of the object</param>
		/// <param name="deleteObjectCompleted">Callback method which will be invoked after delete operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="deleteObjectCompleted"/> is null</exception>
		public void DeleteObject(
			long objectID,
			ObjectType objectType,
			OperationFinished<DeleteObjectResponse> deleteObjectCompleted,
			object userState)
		{
			ThrowIfParameterIsNull(deleteObjectCompleted, "deleteObjectCompleted");

			string type = ObjectType2String(objectType);

			_service.deleteCompleted += DeleteObjectFinished;

			object[] state = {deleteObjectCompleted, userState};

			_service.deleteAsync(_apiKey, AuthenticationToken, type, objectID, state);
		}

		private void DeleteObjectFinished(object sender, deleteCompletedEventArgs e)
		{
			object[] state = (object[]) e.UserState;
			OperationFinished<DeleteObjectResponse> deleteObjectFinishedHandler =
				(OperationFinished<DeleteObjectResponse>)state[0];

			DeleteObjectStatus status = StatusMessageParser.ParseDeleteObjectStatus(e.Result);

			switch (status)
			{
				case DeleteObjectStatus.Successful:
				case DeleteObjectStatus.Failed:
				case DeleteObjectStatus.ApplicationRestricted:
				case DeleteObjectStatus.NotLoggedIn:
					deleteObjectFinishedHandler(status, null);
					break;
				default:
					deleteObjectFinishedHandler(status, e.Result);
					break;
			}
		}
		
		#endregion

		#region GetFolderStructure

		/// <summary>
		/// Retrieves a user's root folder structure
		/// </summary>
		/// <param name="retrieveOptions">Retrieve options</param>
		/// <param name="folder">Root folder</param>
		/// <returns>Operation status</returns>
		public GetAccountTreeStatus GetRootFolderStructure(RetrieveFolderStructureOptions retrieveOptions, out Folder folder)
		{
			return GetFolderStructure(0, retrieveOptions, out folder);
		}

		/// <summary>
		/// Asynchronously retrieves a user's root folder structure
		/// </summary>
		/// <param name="retrieveOptions">Retrieve options</param>
		/// <param name="getFolderStructureCompleted">Callback method which will be executed after operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="getFolderStructureCompleted"/> is null</exception>
		public void GetRootFolderStructure(RetrieveFolderStructureOptions retrieveOptions, OperationFinished<GetAccountTreeStatus, Folder> getFolderStructureCompleted)
		{
			ThrowIfParameterIsNull(getFolderStructureCompleted, "getFolderStructureCompleted");

			GetFolderStructure(0, retrieveOptions, getFolderStructureCompleted);
		}

		/// <summary>
		/// Retrieves a user's folder structure by ID
		/// </summary>
		/// <param name="folderID">ID of the folder to retrieve</param>
		/// <param name="retrieveOptions">Retrieve options</param>
		/// <param name="folder">Folder object</param>
		/// <returns>Operation status</returns>
		public GetAccountTreeStatus GetFolderStructure(long folderID, RetrieveFolderStructureOptions retrieveOptions, out Folder folder)
		{
			folder = null;

			byte[] folderInfoXml;

			string result = _service.get_account_tree(_apiKey, AuthenticationToken, folderID, new string[0], out folderInfoXml);
			GetAccountTreeStatus status = StatusMessageParser.ParseGetAccountTreeStatus(result);

			switch (status)
			{
				case GetAccountTreeStatus.Successful:
					string folderInfo = null;

					if (!retrieveOptions.Contains(RetrieveFolderStructureOptions.NoZip))
					{
						folderInfoXml = Unzip(folderInfoXml);
					}

					if (folderInfoXml != null)
					{
						folderInfo = Encoding.ASCII.GetString(folderInfoXml);
					}

					folder = ParseFolderStructureXmlMessage(folderInfo);
					break;
			}

			return status;
		}

		/// <summary>
		/// Asynchronously retrieves a user's folder structure by ID
		/// </summary>
		/// <param name="folderID">ID of the folder to retrieve</param>
		/// <param name="retrieveOptions">Retrieve options</param>
		/// <param name="getFolderStructureCompleted">Callback method which will be executed after operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="getFolderStructureCompleted"/> is null</exception>
		public void GetFolderStructure(long folderID, RetrieveFolderStructureOptions retrieveOptions, OperationFinished<GetAccountTreeStatus, Folder> getFolderStructureCompleted)
		{
			ThrowIfParameterIsNull(getFolderStructureCompleted, "getFolderStructureCompleted");

			object[] state = new object[2];

			state[0] = retrieveOptions;
			state[1] = getFolderStructureCompleted;

			_service.get_account_treeCompleted += GetFolderStructureFinished;

			_service.get_account_treeAsync(_apiKey, AuthenticationToken, folderID, retrieveOptions.ToStringArray(), state);
		}

		private void GetFolderStructureFinished(object sender, get_account_treeCompletedEventArgs e)
		{
			object[] state = (object[])e.UserState;
			RetrieveFolderStructureOptions retrieveOptions = (RetrieveFolderStructureOptions)state[0];
			OperationFinished<GetAccountTreeStatus, Folder> getFolderStructureFinishedHandler = (OperationFinished<GetAccountTreeStatus, Folder>)state[1];

			GetAccountTreeStatus status = StatusMessageParser.ParseGetAccountTreeStatus(e.Result);
			
			switch (status)
			{
				case GetAccountTreeStatus.Successful:
					byte[] folderInfoXml = null;
					string folderInfo = null;

					if (!retrieveOptions.Contains(RetrieveFolderStructureOptions.NoZip))
					{
						folderInfoXml = Unzip(e.tree);
					}

					if (folderInfoXml != null)
					{
						folderInfo = Encoding.ASCII.GetString(folderInfoXml);
					}

					Folder folder = ParseFolderStructureXmlMessage(folderInfo);

					getFolderStructureFinishedHandler(status, folder, null);
					break;
				case GetAccountTreeStatus.ApplicationRestricted:
				case GetAccountTreeStatus.FolderIDError:
				case GetAccountTreeStatus.NotLoggedID:
					getFolderStructureFinishedHandler(status, null, null);
					break;
				default:
					getFolderStructureFinishedHandler(status, null, e.Result);
					break;
			}
		}
		
		#endregion

		#region ExportTags
		
		/// <summary>
		/// Retrieves list of user's tags
		/// </summary>
		/// <param name="tagList">List of user's tags</param>
		/// <returns>Operation status</returns>
		public ExportTagsStatus ExportTags(out TagPrimitiveCollection tagList)
		{
			byte[] xmlMessage;

			string result = _service.export_tags(_apiKey, AuthenticationToken, out xmlMessage);
			ExportTagsStatus status = StatusMessageParser.ParseExportTagStatus(result);

			tagList = MessageParser.Instance.ParseExportTagsMessage(Encoding.ASCII.GetString(xmlMessage));

			return status;
		}

		/// <summary>
		/// Asynchronously retrieves list of user's tags
		/// </summary>
		/// <param name="exportTagsCompleted">Callback method which will be invioked after operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="exportTagsCompleted"/> is null</exception>
		public void ExportTags(OperationFinished<ExportTagsStatus, TagPrimitiveCollection> exportTagsCompleted)
		{
			ThrowIfParameterIsNull(exportTagsCompleted, "exportTagsCompleted");

			_service.export_tagsCompleted += ExportTagsFinished;

			_service.export_tagsAsync(_apiKey, AuthenticationToken, exportTagsCompleted);
		}

		private void ExportTagsFinished(object sender, export_tagsCompletedEventArgs e)
		{
			OperationFinished<ExportTagsStatus, TagPrimitiveCollection> exportTagsFinishedHandler =
				(OperationFinished<ExportTagsStatus, TagPrimitiveCollection>)e.UserState;

			ExportTagsStatus status = StatusMessageParser.ParseExportTagStatus(e.Result);

			switch (status)
			{
				case ExportTagsStatus.Successful:
					TagPrimitiveCollection tags = MessageParser.Instance.ParseExportTagsMessage(Encoding.ASCII.GetString(e.tag_xml));
					exportTagsFinishedHandler(status, tags, null);
					break;
				case ExportTagsStatus.ApplicationRestricted:
				case ExportTagsStatus.NotLoggedID:
					exportTagsFinishedHandler(status, new TagPrimitiveCollection(), null);
					break;
				default:
					exportTagsFinishedHandler(status, null, e.Result);
					break;
			}
		}
		
		#endregion

		#region GetTag
		private TagPrimitive GetTag(long id)
		{
			ManualResetEvent wait = new ManualResetEvent(false);
			TagPrimitive result = new TagPrimitive();

			OperationFinished<ExportTagsStatus, TagPrimitive> getTagFinishedHandler = (status, tag, errorData) =>
			                                                                          	{
			                                                                          		result = tag;
			                                                                          		wait.Reset();
			                                                                          	};
			GetTag(id, getTagFinishedHandler);
			wait.WaitOne();

			return result;
		}
		private void GetTag(long id, OperationFinished<ExportTagsStatus, TagPrimitive> getTagFinishedHandler)
		{
			if (_tagCollection == null || _tagCollection.IsEmpty)
			{
				OperationFinished<ExportTagsStatus, TagPrimitiveCollection> exportTagsFinishedHandler =
					(status, tags, errorData) =>
						{
							_tagCollection = tags;

							getTagFinishedHandler(status, _tagCollection.GetTag(id), errorData);
						};

				ExportTags(exportTagsFinishedHandler);
			}
			else
			{
				getTagFinishedHandler(ExportTagsStatus.Successful, _tagCollection.GetTag(id), null);
			}
		}
		#endregion

		#region SetDescription

		/// <summary>
		/// Sets description of the object
		/// </summary>
		/// <param name="objectID">ID of the object</param>
		/// <param name="objectType">Object type</param>
		/// <param name="description">Description text</param>
		/// <returns>Operation status</returns>
		public SetDescriptionStatus SetDescription(long objectID, ObjectType objectType, string description)
		{
			string type = ObjectType2String(objectType);

			string result = _service.set_description(_apiKey, AuthenticationToken, type, objectID, description);
			
			return StatusMessageParser.ParseSetDescriptionStatus(result);
		}

		/// <summary>
		/// Asynchronously sets description of the object
		/// </summary>
		/// <param name="objectID">ID of the object</param>
		/// <param name="objectType">Object type</param>
		/// <param name="description">Description text</param>
		/// <param name="setDescriptionCompleted">Callback method which will be invoked after delete operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="setDescriptionCompleted"/> is null</exception>
		public void SetDescription(long objectID, ObjectType objectType, string description, OperationFinished<SetDescriptionStatus> setDescriptionCompleted)
		{
			ThrowIfParameterIsNull(setDescriptionCompleted, "setDescriptionCompleted");

			string type = ObjectType2String(objectType);

			_service.set_descriptionCompleted += SetDescriptionFinished;

			_service.set_descriptionAsync(_apiKey, AuthenticationToken, type, objectID, description, setDescriptionCompleted);
		}

		private void SetDescriptionFinished(object sender, set_descriptionCompletedEventArgs e)
		{
			OperationFinished<SetDescriptionStatus> setDescriptionFinishedHandler = (OperationFinished<SetDescriptionStatus>)e.UserState;

			SetDescriptionStatus status = StatusMessageParser.ParseSetDescriptionStatus(e.Result);

			switch (status)
			{
				case SetDescriptionStatus.Failed:
				case SetDescriptionStatus.Successful:
					setDescriptionFinishedHandler(status, null);
					break;
				default:
					setDescriptionFinishedHandler(status, e.Result);
					break;
			}
		}

		#endregion

		#region Rename

		/// <summary>
		/// Renames specified object
		/// </summary>
		/// <param name="objectID">ID of the object which needs to be renamed</param>
		/// <param name="objectType">Type of the object</param>
		/// <param name="newName">New name of the object</param>
		/// <returns>Operation status</returns>
		public RenameObjectStatus RenameObject(long objectID, ObjectType objectType, string newName)
		{
			string type = ObjectType2String(objectType);
			string result = _service.rename(_apiKey, AuthenticationToken, type, objectID, newName);

			return StatusMessageParser.ParseRenameObjectStatus(result);
		}
		
		/// <summary>
		/// Asynchronously renames specified object
		/// </summary>
		/// <param name="objectID">ID of the object which needs to be renamed</param>
		/// <param name="objectType">Type of the object</param>
		/// <param name="newName">New name of the object</param>
		/// <param name="renameObjectCompleted">Callback method which will be invoked after rename operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="renameObjectCompleted"/> is null</exception>
		public void RenameObject(long objectID, ObjectType objectType, string newName, OperationFinished<RenameObjectStatus> renameObjectCompleted)
		{
			ThrowIfParameterIsNull(renameObjectCompleted, "renameObjectCompleted");

			string type = ObjectType2String(objectType);
			
			_service.renameCompleted += RenameObjectCompleted;

			_service.renameAsync(_apiKey, AuthenticationToken, type, objectID, newName, renameObjectCompleted);
		}

		/// <summary>
		/// Handler method which will be executed after rename operation completes
		/// </summary>
		/// <param name="sender">The source of the event</param>
		/// <param name="e">Argument that contains event data</param>
		private void RenameObjectCompleted(object sender, renameCompletedEventArgs e)
		{
			OperationFinished<RenameObjectStatus> renameObjectFinishedHandler = (OperationFinished<RenameObjectStatus>)e.UserState;

			if(e.Cancelled)
			{
				renameObjectFinishedHandler(RenameObjectStatus.Failed, null);
			}

			RenameObjectStatus status = StatusMessageParser.ParseRenameObjectStatus(e.Result);
			string errorData = status == RenameObjectStatus.Unknown ? e.Result : null;
				
			renameObjectFinishedHandler(status, errorData);
		}
		
		#endregion

		#region Move
		
		/// <summary>
		/// Moves object from one folder to another one
		/// </summary>
		/// <param name="targetObjectID">ID of the object which needs to be moved</param>
		/// <param name="targetObjectType">Type of the object</param>
		/// <param name="destinationFolderID">ID of the destination folder</param>
		/// <returns>Operation status</returns>
		public MoveObjectStatus MoveObject(long targetObjectID, ObjectType targetObjectType, long destinationFolderID)
		{
			string type = ObjectType2String(targetObjectType);
			string result = _service.move(_apiKey, AuthenticationToken, type, targetObjectID, destinationFolderID);

			return StatusMessageParser.ParseMoveObjectStatus(result);
		}
		
		/// <summary>
		/// Asynchronously moves object from one folder to another one
		/// </summary>
		/// <param name="targetObjectID">ID of the object which needs to be moved</param>
		/// <param name="targetObjectType">Type of the object</param>
		/// <param name="destinationFolderID">ID of the destination folder</param>
		/// <param name="moveObjectCompleted">Callback method which will be invoked after move operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="moveObjectCompleted"/> is null</exception>
		public void MoveObject(long targetObjectID, ObjectType targetObjectType, long destinationFolderID, OperationFinished<MoveObjectStatus> moveObjectCompleted)
		{
			ThrowIfParameterIsNull(moveObjectCompleted, "moveObjectCompleted");

			string type = ObjectType2String(targetObjectType);

			_service.moveCompleted += MoveObjectFinished;

			_service.moveAsync(_apiKey, AuthenticationToken, type, targetObjectID, destinationFolderID, moveObjectCompleted);
		}

		private void MoveObjectFinished(object sender, moveCompletedEventArgs e)
		{
			OperationFinished<MoveObjectStatus> moveObjectFinishedHandler = (OperationFinished<MoveObjectStatus>)e.UserState;

			MoveObjectStatus status = StatusMessageParser.ParseMoveObjectStatus(e.Result);

			switch (status)
			{
				case MoveObjectStatus.Successful:
				case MoveObjectStatus.ApplicationRestricted:
				case MoveObjectStatus.Failed:
				case MoveObjectStatus.NotLoggedIn:
					moveObjectFinishedHandler(status, null);
					break;
				default:
					moveObjectFinishedHandler(status, e.Result);
					break;
			}
		}
		
		#endregion

		#region Logout

		/// <summary>
		/// Logouts current user
		/// </summary>
		/// <returns>Operation status</returns>
		public LogoutStatus Logout()
		{
			string result = _service.logout(_apiKey, AuthenticationToken);

			return StatusMessageParser.ParseLogoutStatus(result);
		}

		/// <summary>
		/// Asynchronously logouts current user
		/// </summary>
		/// <param name="logoutCompleted">Callback method which will be invoked after logout operation completes</param>
		public void Logout(OperationFinished<LogoutStatus> logoutCompleted)
		{
			ThrowIfParameterIsNull(logoutCompleted, "logoutCompleted");

			_service.logoutCompleted += LogoutFinished;

			_service.logoutAsync(_apiKey, AuthenticationToken, logoutCompleted);
		}

		private void LogoutFinished(object sender, logoutCompletedEventArgs e)
		{
			OperationFinished<LogoutStatus> logoutFinishedHandler = (OperationFinished<LogoutStatus>)e.UserState;

			LogoutStatus status = StatusMessageParser.ParseLogoutStatus(e.Result);

			switch (status)
			{
				case LogoutStatus.Successful:
				case LogoutStatus.InvalidAuthToken:
					logoutFinishedHandler(status, null);
					break;
				case LogoutStatus.Unknown:
					logoutFinishedHandler(status, e.Result);
					break;
			}
		}
		#endregion

		#region RegisterNewUser

		/// <summary>
		/// Registers new Box.NET user
		/// </summary>
		/// <param name="login">Account login name</param>
		/// <param name="password">Account password</param>
		/// <param name="response">Contains information about user account and valid authorization token</param>
		/// <returns>Operation status</returns>
		public RegisterNewUserStatus RegisterNewUser(string login, string password, out RegisterNewUserResponse response)
		{
			string token;
			SOAPUser user;

			string result = _service.register_new_user(_apiKey, login, password, out token, out user);

			response = new RegisterNewUserResponse {Token = token, User = user};

			return StatusMessageParser.ParseRegisterNewUserStatus(result);
		}
		
		/// <summary>
		/// Asynchronously registers new Box.NET user
		/// </summary>
		/// <param name="login">Account login name</param>
		/// <param name="password">Account password</param>
		/// <param name="registerNewUserCompleted">Callback method which will be invoked after operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="registerNewUserCompleted"/> is null</exception>
		public void RegisterNewUser(string login, string password, OperationFinished<RegisterNewUserStatus, RegisterNewUserResponse> registerNewUserCompleted)
		{
			ThrowIfParameterIsNull(registerNewUserCompleted, "registerNewUserCompleted");

			_service.register_new_userCompleted += RegisterNewUserFinished;

			_service.register_new_userAsync(_apiKey, login, password, registerNewUserCompleted);
		}

		private void RegisterNewUserFinished(object sender, register_new_userCompletedEventArgs e)
		{
			RegisterNewUserResponse response = new RegisterNewUserResponse {Token = e.auth_token, User = e.user};
			RegisterNewUserStatus status = StatusMessageParser.ParseRegisterNewUserStatus(e.Result);
			
			OperationFinished<RegisterNewUserStatus, RegisterNewUserResponse> registerNewUserFinishedHandler =
				(OperationFinished<RegisterNewUserStatus, RegisterNewUserResponse>)e.UserState;

			switch (status)
			{
				case RegisterNewUserStatus.Successful:
				case RegisterNewUserStatus.ApplicationRestricted:
				case RegisterNewUserStatus.EmailAlreadyRegistered:
				case RegisterNewUserStatus.EmailInvalid:
				case RegisterNewUserStatus.Failed:
					registerNewUserFinishedHandler(status, response, null);
					break;
				default:
					registerNewUserFinishedHandler(status, response, e.Result);
					break;
			}
		}

		#endregion

		#region VerifyRegistrationEmail

		/// <summary>
		/// Verifies registration email address
		/// </summary>
		/// <param name="login">Account login name</param>
		/// <returns>Operation status</returns>
		public VerifyRegistrationEmailStatus VerifyRegistrationEmail(string login)
		{
			string result = _service.verify_registration_email(_apiKey, login);

			return StatusMessageParser.ParseVerifyRegistrationEmailStatus(result);
		}

		/// <summary>
		/// Asynchronously verifies registration email address
		/// </summary>
		/// <param name="login">Account login name</param>
		/// <param name="verifyRegistrationEmailCompleted">Callback method which will be invoked after operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="verifyRegistrationEmailCompleted"/> is null</exception>
		public void VerifyRegistrationEmail(string login, OperationFinished<VerifyRegistrationEmailStatus> verifyRegistrationEmailCompleted)
		{
			ThrowIfParameterIsNull(verifyRegistrationEmailCompleted, "verifyRegistrationEmailCompleted");

			_service.verify_registration_emailCompleted += VerifyRegistrationEmail;

			_service.verify_registration_emailAsync(_apiKey, login, verifyRegistrationEmailCompleted);
		}

		private void VerifyRegistrationEmail(object sender, verify_registration_emailCompletedEventArgs e)
		{
			VerifyRegistrationEmailStatus status = StatusMessageParser.ParseVerifyRegistrationEmailStatus(e.Result);
			OperationFinished<VerifyRegistrationEmailStatus> verifyRegistrationEmailFinishedHandler = (OperationFinished<VerifyRegistrationEmailStatus>)e.UserState;

			switch (status)
			{
				case VerifyRegistrationEmailStatus.EmailOK:
				case VerifyRegistrationEmailStatus.ApplicationRestricted:
				case VerifyRegistrationEmailStatus.EmailInvalid:
				case VerifyRegistrationEmailStatus.EmailAlreadyRegistered:
					verifyRegistrationEmailFinishedHandler(status, null);
					break;
				default:
					verifyRegistrationEmailFinishedHandler(status, e.Result);
					break;
			}
		}
		
		#endregion

		#region AddToMyBox

		/// <summary>
		/// Copies a file publicly shared by someone to a user's folder
		/// </summary>
		/// <param name="targetFileID">ID of the file which needs to be copied</param>
		/// <param name="destinationFolderID">ID of the destination folder</param>
		/// <param name="tagList">Tags which need to be assigned to the target file</param>
		/// <returns>Operation status</returns>
		public AddToMyBoxStatus AddToMyBox(long targetFileID, long destinationFolderID, TagPrimitiveCollection tagList)
		{
			string result = _service.add_to_mybox(_apiKey, AuthenticationToken, targetFileID, null, destinationFolderID,
			                      ConvertTagPrimitiveCollection2String(tagList));

			return StatusMessageParser.ParseAddToMyBoxStatus(result);
		}

		/// <summary>
		/// Copies a file publicly shared by someone to a user's folder
		/// </summary>
		/// <param name="targetFileName">Name of the file which needs to be copied</param>
		/// <param name="destinationFolderID">ID of the destination folder</param>
		/// <param name="tagList">Tags which need to be assigned to the target file</param>
		/// <returns>Operation status</returns>
		public AddToMyBoxStatus AddToMyBox(string targetFileName, long destinationFolderID, TagPrimitiveCollection tagList)
		{
			string result = _service.add_to_mybox(_apiKey, AuthenticationToken, 0, targetFileName, destinationFolderID,
								  ConvertTagPrimitiveCollection2String(tagList));

			return StatusMessageParser.ParseAddToMyBoxStatus(result);
		}

		/// <summary>
		/// Asuncronously copies a file publicly shared by someone to a user's folder
		/// </summary>
		/// <param name="targetFileID">ID of the file which needs to be copied</param>
		/// <param name="destinationFolderID">ID of the destination folder</param>
		/// <param name="tagList">Tags which need to be assigned to the target file</param>
		/// <param name="addToMyBoxCompleted">Delegate which will be executed after operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="addToMyBoxCompleted"/> is null</exception>
		public void AddToMyBox(long targetFileID, long destinationFolderID, TagPrimitiveCollection tagList, OperationFinished<AddToMyBoxStatus> addToMyBoxCompleted)
		{
			ThrowIfParameterIsNull(addToMyBoxCompleted, "addToMyBoxCompleted");

			_service.add_to_myboxCompleted += AddToMyBoxFinished;

			_service.add_to_myboxAsync(_apiKey, AuthenticationToken, targetFileID, null, destinationFolderID, ConvertTagPrimitiveCollection2String(tagList), addToMyBoxCompleted);
		}

		/// <summary>
		/// Asuncronously copies a file publicly shared by someone to a user's folder
		/// </summary>
		/// <param name="targetFileName">Name of the file which needs to be copied</param>
		/// <param name="destinationFolderID">ID of the destination folder</param>
		/// <param name="tagList">Tags which need to be assigned to the target file</param>
		/// <param name="addToMyBoxCompleted">Callback method which will be invoked after operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="addToMyBoxCompleted"/> is null</exception>
		public void AddToMyBox(string targetFileName, long destinationFolderID, TagPrimitiveCollection tagList, OperationFinished<AddToMyBoxStatus> addToMyBoxCompleted)
		{
			ThrowIfParameterIsNull(addToMyBoxCompleted, "addToMyBoxCompleted");

			_service.add_to_myboxCompleted += AddToMyBoxFinished;

			_service.add_to_myboxAsync(_apiKey, AuthenticationToken, 0, targetFileName, destinationFolderID, ConvertTagPrimitiveCollection2String(tagList), addToMyBoxCompleted);
		}

		private void AddToMyBoxFinished(object sender, add_to_myboxCompletedEventArgs e)
		{
			OperationFinished<AddToMyBoxStatus> addToMyBoxCompleted = (OperationFinished<AddToMyBoxStatus>) e.UserState;
			AddToMyBoxStatus status = StatusMessageParser.ParseAddToMyBoxStatus(e.Result);

			switch (status)
			{
				case AddToMyBoxStatus.ApplicationRestricted:
				case AddToMyBoxStatus.Failed:
				case AddToMyBoxStatus.LinkExists:
				case AddToMyBoxStatus.NotLoggedIn:
				case AddToMyBoxStatus.Successful:
					addToMyBoxCompleted(status, null);
					break;
				default:
					addToMyBoxCompleted(status, e.Result);
					break;
			}
		}

		#endregion

		#region PublicShare

		/// <summary>
		/// Publicly shares a file or folder
		/// </summary>
		/// <param name="targetObjectID">ID of the object to be shared</param>
		/// <param name="targetObjectType">Type of the object</param>
		/// <param name="password">Password to protect shared object or Null</param>
		/// <param name="notificationMessage">Message to be included in a notification email</param>
		/// <param name="emailList">Array of emails for which to notify users about a newly shared file or folder</param>
		/// <param name="publicName">Unique identifier of a publicly shared object</param>
		/// <returns>Operation status</returns>
		public PublicShareStatus PublicShare(long targetObjectID, ObjectType targetObjectType, string password, string notificationMessage, string[] emailList, out string publicName)
		{
			string type = ObjectType2String(targetObjectType);
			string result = _service.public_share(_apiKey, AuthenticationToken, type, targetObjectID, password, notificationMessage, emailList, out publicName);

			return StatusMessageParser.ParsePublicShareStatus(result);
		}

		/// <summary>
		/// Asynchronously publicly shares a file or folder
		/// </summary>
		/// <param name="targetObjectID">ID of the object to be shared</param>
		/// <param name="targetObjectType">Type of the object</param>
		/// <param name="password">Password to protect shared object or Null</param>
		/// <param name="message">Message to be included in a notification email</param>
		/// <param name="emailList">Array of emails for which to notify users about a newly shared file or folder</param>
		/// <param name="sendNotification">Indicates if the notification about object sharing must be send</param>
		/// <param name="publicShareCompleted">Callback method which will be invoked after operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="publicShareCompleted"/> is null</exception>
		public void PublicShare(long targetObjectID, ObjectType targetObjectType, string password, string message, string[] emailList, bool sendNotification, OperationFinished<PublicShareStatus, string> publicShareCompleted)
		{
			ThrowIfParameterIsNull(publicShareCompleted, "publicShareCompleted");

			string type = ObjectType2String(targetObjectType);

			_service.public_shareCompleted += PublicShareFinished;
			_service.private_shareAsync(_apiKey, AuthenticationToken, type, targetObjectID, emailList, message, sendNotification, publicShareCompleted);
		}

		private void PublicShareFinished(object sender, public_shareCompletedEventArgs e)
		{
			OperationFinished<PublicShareStatus, string> publicShareCompleted = (OperationFinished<PublicShareStatus, string>)e.UserState;
			PublicShareStatus status = StatusMessageParser.ParsePublicShareStatus(e.Result);

			switch (status)
			{
				case PublicShareStatus.Successful:
				case PublicShareStatus.Failed:
				case PublicShareStatus.ApplicationRestricted:
				case PublicShareStatus.NotLoggedIn:
				case PublicShareStatus.WrongNode:
					publicShareCompleted(status, e.public_name, null);
					break;
				default:
					publicShareCompleted(status, e.public_name, e.Result);
					break;
			}
		}

		#endregion

		#region PublicUnshare

		/// <summary>
		/// Unshares a shared object
		/// </summary>
		/// <param name="targetObjectID">ID of the object to be unshared</param>
		/// <param name="targetObjectType">Type of the object</param>
		/// <returns>Operation status</returns>
		public PublicUnshareStatus PublicUnshare(long targetObjectID, ObjectType targetObjectType)
		{
			string type = ObjectType2String(targetObjectType);
			string result = _service.public_unshare(_apiKey, AuthenticationToken, type, targetObjectID);

			return StatusMessageParser.ParsePublicUnshareStatus(result);
		}

		/// <summary>
		/// Asynchronously unshares a shared object
		/// </summary>
		/// <param name="targetObjectID">ID of the object to be unshared</param>
		/// <param name="targetObjectType">Type of the object</param>
		/// <param name="publicUnshareCompleted">Callback method which will be invoked after operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="publicUnshareCompleted"/> is null</exception>
		public void PublicUnshare(long targetObjectID, ObjectType targetObjectType, OperationFinished<PublicUnshareStatus> publicUnshareCompleted)
		{
			ThrowIfParameterIsNull(publicUnshareCompleted, "publicUnshareCompleted");

			string type = ObjectType2String(targetObjectType);
			
			_service.public_unshareCompleted += PublicUnshareCompleted;

			_service.public_unshareAsync(_apiKey, AuthenticationToken, type, targetObjectID, publicUnshareCompleted);
		}

		private void PublicUnshareCompleted(object sender, public_unshareCompletedEventArgs e)
		{
			OperationFinished<PublicUnshareStatus> publicUnshareCompleted = (OperationFinished<PublicUnshareStatus>) e.UserState;
			PublicUnshareStatus status = StatusMessageParser.ParsePublicUnshareStatus(e.Result);

			switch (status)
			{
				case PublicUnshareStatus.Successful:
				case PublicUnshareStatus.Failed:
				case PublicUnshareStatus.NotLoggedIn:
				case PublicUnshareStatus.WrongNode:
				case PublicUnshareStatus.ApplicationRestricted:
					publicUnshareCompleted(status, null);
					break;
				default:
					publicUnshareCompleted(status, e.Result);
					break;
			}
		}

		#endregion

		#region PrivateShare

		/// <summary>
		/// Privately shares an object with another user(s)
		/// </summary>
		/// <param name="targetObjectID">ID of the object to be shared</param>
		/// <param name="targetObjectType">Type of the object</param>
		/// <param name="password">Password to protect shared object or Null</param>
		/// <param name="notificationMessage">Message to be included in a notification email</param>
		/// <param name="emailList">Array of emails for which to notify users about a newly shared file or folder</param>
		/// <param name="sendNotification">Indicates if the notification about object sharing must be send</param>
		/// <returns>Operation status</returns>
		public PrivateShareStatus PrivateShare(long targetObjectID, ObjectType targetObjectType, string password, string notificationMessage, string[] emailList, bool sendNotification)
		{
			string type = ObjectType2String(targetObjectType);
			string result = _service.private_share(_apiKey, AuthenticationToken, type, targetObjectID, emailList, notificationMessage, sendNotification);

			return StatusMessageParser.ParsePrivateShareStatus(result);
		}

		/// <summary>
		/// Asynchronously privately shares an object with another user(s)
		/// </summary>
		/// <param name="targetObjectID">ID of the object to be shared</param>
		/// <param name="targetObjectType">Type of the object</param>
		/// <param name="password">Password to protect shared object or Null</param>
		/// <param name="notificationMessage">Message to be included in a notification email</param>
		/// <param name="emailList">Array of emails for which to notify users about a newly shared file or folder</param>
		/// <param name="sendNotification">Indicates if the notification about object sharing must be send</param>
		/// <param name="privateShareCompleted">Callback method which will be invoked after operation completes</param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="privateShareCompleted"/> is null</exception>
		public void PrivateShare(long targetObjectID, ObjectType targetObjectType, string password, string notificationMessage, string[] emailList, bool sendNotification, OperationFinished<PrivateShareResponse> privateShareCompleted)
		{
			PrivateShare(targetObjectID, targetObjectType, password, notificationMessage, emailList, sendNotification,
			             privateShareCompleted, null);
		}

		/// <summary>
		/// Asynchronously privately shares an object with another user(s)
		/// </summary>
		/// <param name="targetObjectID">ID of the object to be shared</param>
		/// <param name="targetObjectType">Type of the object</param>
		/// <param name="password">Password to protect shared object or Null</param>
		/// <param name="notificationMessage">Message to be included in a notification email</param>
		/// <param name="emailList">Array of emails for which to notify users about a newly shared file or folder</param>
		/// <param name="sendNotification">Indicates if the notification about object sharing must be send</param>
		/// <param name="privateShareCompleted">Callback method which will be invoked after operation completes</param>
		/// <param name="userState"></param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="privateShareCompleted"/> is null</exception>
		public void PrivateShare(long targetObjectID, ObjectType targetObjectType, string password, string notificationMessage, string[] emailList, bool sendNotification, OperationFinished<PrivateShareResponse> privateShareCompleted, object userState)
		{
			ThrowIfParameterIsNull(privateShareCompleted, "privateShareCompleted");
			
			string type = ObjectType2String(targetObjectType);

			_service.private_shareCompleted += PrivateShareFinished;

			object[] data = {userState, privateShareCompleted};

			_service.private_shareAsync(_apiKey, AuthenticationToken, type, targetObjectID, emailList, notificationMessage, sendNotification, data);
		}

		private void PrivateShareFinished(object sender, private_shareCompletedEventArgs e)
		{
			object[] userState = (object[])e.UserState;
			OperationFinished<PrivateShareResponse> privateShareCompleted = (OperationFinished<PrivateShareResponse>)userState[1];
			PrivateShareStatus status = StatusMessageParser.ParsePrivateShareStatus(e.Result);

			PrivateShareResponse response = new PrivateShareResponse { Status = status, UserState = userState[0] };

			switch (status)
			{
				case PrivateShareStatus.Successful:
				case PrivateShareStatus.Failed:
				case PrivateShareStatus.ApplicationRestricted:
				case PrivateShareStatus.NotLoggedIn:
				case PrivateShareStatus.WrongNode:
					privateShareCompleted(response, null);
					break;
				default:
					privateShareCompleted(response, e.Result);
					break;
			}
		}

		#endregion


		#region Helper method

		/// <summary>
		/// Throws ArgumentException if <paramref name="parameter"/> is null
		/// </summary>
		/// <param name="parameter">Parameter which needs to be checked</param>
		/// <param name="parameterName">Parameter name</param>
		internal static void ThrowIfParameterIsNull(object parameter, string parameterName)
		{
			if (parameter == null)
			{
				throw new ArgumentException(string.Format("'{0}' can not be null", parameterName));
			}
		}


		/// <summary>
		/// Converts list of tags to comma-separated string which contains tags' IDs
		/// </summary>
		/// <param name="tagList">List of tags</param>
		/// <returns>Comma-separated string which contains tags' IDs</returns>
		private static string ConvertTagPrimitiveCollection2String(TagPrimitiveCollection tagList)
		{
			StringBuilder result = new StringBuilder();

			foreach (TagPrimitive tag in tagList)
			{
				result.Append(tag.ID + ",");
			}

			if (result.Length > 0)
			{
				result.Remove(result.Length - 1, 1);
			}

			return result.ToString();
		}

		/// <summary>
		/// Converts <paramref name="objectType"/> to string representation
		/// </summary>
		/// <param name="objectType">Object type</param>
		/// <returns>String representation of <paramref name="objectType"/> variable</returns>
		/// <exception cref="NotSupportedObjectTypeException">Thrown when method can't convert <paramref name="objectType"/> variable to String</exception>
		private static string ObjectType2String(ObjectType objectType)
		{
			string type;

			switch (objectType)
			{
				case ObjectType.File:
					type = "file";
					break;
				case ObjectType.Folder:
					type = "folder";
					break;
				default:
					throw new NotSupportedObjectTypeException(objectType);
			}

			return type;
		}

		/// <summary>
		/// Parses XML folder structure message
		/// </summary>
		/// <param name="message">Folder structure message</param>
		/// <returns>Parsed folder structure</returns>
		private Folder ParseFolderStructureXmlMessage(string message)
		{
			Expression<Func<long, TagPrimitive>> materializeTag = tagID => GetTag(tagID);

			return MessageParser.Instance.ParseFolderStructureMessage(message, materializeTag);
		}

		/// <summary>
		/// Extracts first file from zip archive
		/// </summary>
		/// <param name="input">ZIP archive content</param>
		/// <returns>Content of the first ZIPed file or empty byte array</returns>
		private static byte[] Unzip(byte[] input)
		{
			byte[] output;
			byte[] buffer = new byte[1024];

			using (MemoryStream resultStream = new MemoryStream())
			{
				using (MemoryStream inputStream = new MemoryStream())
				{
					inputStream.Write(input, 0, input.Length);
					inputStream.Flush();
					inputStream.Seek(0, SeekOrigin.Begin);
					
					ZipFile zipArchive = new ZipFile(inputStream);

					if (zipArchive.Count > 0 && zipArchive[0].IsFile && zipArchive[0].CanDecompress)
					{
						using (Stream decompressor = zipArchive.GetInputStream(0))
						{
							int readBytes;

							while ((readBytes = decompressor.Read(buffer, 0, buffer.Length)) != 0)
							{
								resultStream.Write(buffer, 0, readBytes);
							}

							decompressor.Close();
						}
					}

					zipArchive.Close();
					
					inputStream.Close();
				}

				output = new byte[resultStream.Length];

				resultStream.Flush();
				resultStream.Seek(0, SeekOrigin.Begin);
				resultStream.Read(output, 0, output.Length);

				resultStream.Close();
			}

			return output;
		}

		#endregion
	}
}
