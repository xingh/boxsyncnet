﻿using BoxSync.Core.Statuses;


namespace BoxSync.Core
{
	/// <summary>
	/// Defines methods for parsing values of status fields
	/// </summary>
	internal static class StatusMessageParser
	{
		internal static AuthorizeStatus ParseAuthorizeStatus(string status)
		{
			switch (status)
			{
				default:
					return AuthorizeStatus.Unknown;
			}
		}

		internal static CreateFolderStatus ParseAddFolderStatus(string status)
		{
			switch (status)
			{
				case "create_ok":
					return CreateFolderStatus.Successful;
				case "e_no_parent_folder":
					return CreateFolderStatus.NoParentFolder;
				case "not_logged_in":
					return CreateFolderStatus.NotLoggedIn;
				case "application_restricted":
					return CreateFolderStatus.ApplicationRestricted;
				default:
					return CreateFolderStatus.Unknown;
			}
		}

		internal static UploadFileStatus ParseUploadFileStatus(string status)
		{
			switch (status)
			{
				case "upload_ok":
					return UploadFileStatus.Successful;
				case "application_restricted":
					return UploadFileStatus.ApplicationRestricted;
				case "not_logged_id":
					return UploadFileStatus.NotLoggedID;
				case "upload_some_files_failed":
					return UploadFileStatus.Failed;
				default:
					return UploadFileStatus.Unknown;
			}
		}

		internal static DeleteObjectStatus ParseDeleteObjectStatus(string status)
		{
			switch (status)
			{
				case "s_delete_node":
					return DeleteObjectStatus.Successful;
				case "e_delete_node":
					return DeleteObjectStatus.Failed;
				case "not_logged_in":
					return DeleteObjectStatus.NotLoggedIn;
				case "application_restricted":
					return DeleteObjectStatus.ApplicationRestricted;
				default:
					return DeleteObjectStatus.Unknown;
			}
		}

		internal static ExportTagsStatus ParseExportTagStatus(string status)
		{
			switch (status)
			{
				case "export_tags_ok":
					return ExportTagsStatus.Successful;
				case "application_restricted":
					return ExportTagsStatus.ApplicationRestricted;
				case "not_logged_id":
					return ExportTagsStatus.NotLoggedID;
				default:
					return ExportTagsStatus.Unknown;
			}
		}

		internal static SetDescriptionStatus ParseSetDescriptionStatus(string status)
		{
			switch (status)
			{
				case "s_set_description":
					return SetDescriptionStatus.Successful;
				case "e_set_description":
					return SetDescriptionStatus.Failed;
				default:
					return SetDescriptionStatus.Unknown;
			}
		}

		internal static GetAccountTreeStatus ParseGetAccountTreeStatus(string status)
		{
			switch (status)
			{
				case "listing_ok":
					return GetAccountTreeStatus.Successful;
				case "not_logged_id":
					return GetAccountTreeStatus.NotLoggedID;
				case "application_restricted":
					return GetAccountTreeStatus.ApplicationRestricted;
				case "e_folder_id":
					return GetAccountTreeStatus.FolderIDError;
				default:
					return GetAccountTreeStatus.Unknown;
			}
		}

		internal static GetTicketStatus ParseGetTicketStatus(string status)
		{
			switch (status)
			{
				case "get_ticket_ok":
					return GetTicketStatus.Successful;
				default:
					return GetTicketStatus.Unknown;
			}
		}

		internal static GetAuthenticationTokenStatus ParseGetAuthenticationTockenStatus(string status)
		{
			switch (status)
			{
				case "get_auth_token_error":
					return GetAuthenticationTokenStatus.Failed;
				case "get_auth_token_ok":
					return GetAuthenticationTokenStatus.Successful;
				default:
					return GetAuthenticationTokenStatus.Unknown;
			}
		}

		internal static RenameObjectStatus ParseRenameObjectStatus(string status)
		{
			switch (status)
			{
				case "s_rename_node":
					return RenameObjectStatus.Successful;
				case "e_rename_node":
					return RenameObjectStatus.Failed;
				case "application_restricted":
					return RenameObjectStatus.ApplicationRestricted;
				case "not_logged_in":
					return RenameObjectStatus.NotLoggedIn;
				default:
					return RenameObjectStatus.Unknown;
			}
		}

		internal static MoveObjectStatus ParseMoveObjectStatus(string status)
		{
			switch (status)
			{
				case "s_move_node":
					return MoveObjectStatus.Successful;
				case "e_move_node":
					return MoveObjectStatus.Failed;
				case "application_restricted":
					return MoveObjectStatus.ApplicationRestricted;
				case "not_logged_in":
					return MoveObjectStatus.NotLoggedIn;
				default:
					return MoveObjectStatus.Unknown;
			}
		}

		internal static CopyObjectStatus ParseCopyObjectStatus(string status)
		{
			switch (status)
			{
				case "s_copy_node":
					return CopyObjectStatus.Successful;
				case "e_copy_node":
					return CopyObjectStatus.Failed;
				default:
					return CopyObjectStatus.Unknown;
			}
		}

		internal static LogoutStatus ParseLogoutStatus(string status)
		{
			switch (status)
			{
				case "logout_ok":
					return LogoutStatus.Successful;
				case "not_logged_in":
					return LogoutStatus.NotLoggedID;
				case "application_restricted":
					return LogoutStatus.ApplicationRestricted;
				case "invalid_auth_token":
					return LogoutStatus.InvalidAuthToken;
				default:
					return LogoutStatus.Unknown;
			}
		}

		internal static RegisterNewUserStatus ParseRegisterNewUserStatus(string status)
		{
			switch (status)
			{
				case "successful_register":
					return RegisterNewUserStatus.Successful;
				case "e_register":
					return RegisterNewUserStatus.Failed;
				case "email_invalid":
					return RegisterNewUserStatus.EmailInvalid;
				case "email_already_registered":
					return RegisterNewUserStatus.EmailAlreadyRegistered;
				case "application_restricted":
					return RegisterNewUserStatus.ApplicationRestricted;
				default:
					return RegisterNewUserStatus.Unknown;
			}
		}

		internal static VerifyRegistrationEmailStatus ParseVerifyRegistrationEmailStatus(string status)
		{
			switch (status)
			{
				case "email_ok":
					return VerifyRegistrationEmailStatus.EmailOK;
				case "email_invalid":
					return VerifyRegistrationEmailStatus.EmailInvalid;
				case "email_already_registered":
					return VerifyRegistrationEmailStatus.EmailAlreadyRegistered;
				case "application_restricted":
					return VerifyRegistrationEmailStatus.ApplicationRestricted;
				default:
					return VerifyRegistrationEmailStatus.Unknown;
			}
		}

		internal static PublicShareStatus ParsePublicShareStatus(string status)
		{
			switch (status)
			{
				case "share_ok":
					return PublicShareStatus.Successful;
				case "share_error":
					return PublicShareStatus.Failed;
				case "wrong_node":
					return PublicShareStatus.WrongNode;
				case "not_logged_in":
					return PublicShareStatus.NotLoggedIn;
				case "application_restricted":
					return PublicShareStatus.ApplicationRestricted;
				default:
					return PublicShareStatus.Unknown;
			}
		}

		internal static PublicUnshareStatus ParsePublicUnshareStatus(string status)
		{
			switch (status)
			{
				case "unshare_ok":
					return PublicUnshareStatus.Successful;
				case "unshare_error":
					return PublicUnshareStatus.Failed;
				case "wrong_node":
					return PublicUnshareStatus.WrongNode;
				case "not_logged_in":
					return PublicUnshareStatus.NotLoggedIn;
				case "application_restricted":
					return PublicUnshareStatus.ApplicationRestricted;
				default:
					return PublicUnshareStatus.Unknown;
			}
		}

		internal static PrivateShareStatus ParsePrivateShareStatus(string status)
		{
			switch (status)
			{
				case "private_share_ok":
					return PrivateShareStatus.Successful;
				case "private_share_error":
					return PrivateShareStatus.Failed;
				case "wrong_node":
					return PrivateShareStatus.WrongNode;
				case "not_logged_in":
					return PrivateShareStatus.NotLoggedIn;
				case "application_restricted":
					return PrivateShareStatus.ApplicationRestricted;
				default:
					return PrivateShareStatus.Unknown;
			}
		}

		internal static AddToMyBoxStatus ParseAddToMyBoxStatus(string status)
		{
			switch (status)
			{
				case "addtomybox_ok":
					return AddToMyBoxStatus.Successful;
				case "addtomybox_error":
					return AddToMyBoxStatus.Failed;
				case "not_logged_id":
					return AddToMyBoxStatus.NotLoggedIn;
				case "application_restricted":
					return AddToMyBoxStatus.ApplicationRestricted;
				case "s_link_exists":
					return AddToMyBoxStatus.LinkExists;
				default:
					return AddToMyBoxStatus.Unknown;
			}
		}
	}
}
