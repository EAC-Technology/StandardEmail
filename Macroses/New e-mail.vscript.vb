'--- General libs
use lib_utils
use lib_dialog_2

'Initial SetUp
args = xml_dialog.get_answer
logger ("Data sent :" & tojson(args))
logger Appinmail.utils.parseEmails(args)

set current_user = ProAdmin.current_user

if "step" in args then
  if args( "step" )="" then
    operation_step ="Init"
  else
    operation_step = args( "step" )
  end if
else
  operation_step ="Init"
end if

'###############################################################################################
'#                                Screen Definition                                            #
'###############################################################################################
'VDOMData = DynamicVDOM.render(vdomxml) 
set MyForms = New XMLDialogBuilder

'-------------------------------------- VDOM Screen TEST ---------------------------------------
' TEST to include VDOM XML in XML Dialog
'-----------------------------------------------------------------------------------------------    
    MyForms.addScreen("Show VDOM")
    MyForms.Screen("Show VDOM").addComponent("VDOMXML","hypertext")
    
    vdomxml =  "<TEXT name='test' value='test'/>"
   
    DynamicVDOM.left="0"
    DynamicVDOM.top="0"
    DynamicVDOM.width="600"
    DynamicVDOM.width="300"
    'VDOMData = DynamicVDOM.render(vdomxml) 
    'MyForms.Screen("Show VDOM").Component("VDOMXML").value(VDOMData)

    'logger Replace(VDOMData, "<", "&lt;")
    
'-------------------------------------- Std Composer -------------------------------------------
' Main error Window
'-----------------------------------------------------------------------------------------------    
  MyForms.addScreen("Std Composer")
  MyForms.Screen("Std Composer").width  = 800
  MyForms.Screen("Std Composer").Height = 800
  MyForms.Screen("Std Composer").Title  = "Standard mail composer"
  'MyForms.Screen("Std Composer").addComponent("fromemail","livesearch")
  'MyForms.Screen("Std Composer").Component("fromemail").label("From :")
  MyForms.Screen("Std Composer").addComponent("toemail","livesearch")
  MyForms.Screen("Std Composer").Component("toemail").label("To :")
  MyForms.Screen("Std Composer").Component("toemail").sourceURI("/recipientlivesearch")
  MyForms.Screen("Std Composer").addComponent("subject","TextBox")
  MyForms.Screen("Std Composer").Component("subject").label("Subject :")
  MyForms.Screen("Std Composer").addComponent("message","RichTextArea")
  MyForms.Screen("Std Composer").Component("message").label("Message :")
  MyForms.Screen("Std Composer").Component("message").height("350")
  MyForms.Screen("Std Composer").addComponent("attach","FileUpload")
  MyForms.Screen("Std Composer").Component("attach").label("Attachments :")
  
  MyForms.Screen("Std Composer").addComponent("btns","btngroup")
  MyForms.Screen("Std Composer").Component("btns").addBtn("Send","sendEmail")
  MyForms.Screen("Std Composer").Component("btns").addBtn("Cancel","Exit")


Select case ProMail.mailcomposer_command

  case "reply"
    MyForms.Screen("Std Composer").Component("subject").defaultValue("RE: " & ProMail.selected_mail.subject)
    if ProMail.selected_mail.sender.name = "" then
        sender_email = ProMail.selected_mail.sender.email
    else
        sender_email = ProMail.selected_mail.sender.name & " <" & ProMail.selected_mail.sender.email & ">"
    end if
    MyForms.Screen("Std Composer").Component("toemail").defaultValue(sender_email)
    sender_name = ProMail.selected_mail.sender.name
    if sender_name = "" then
        sender_name = ProMail.selected_mail.sender.email
    end if
    MyForms.Screen("Std Composer").Component("message").defaultValue("<br><br>In reply to message from " & sender_name & " on " & ProMail.selected_mail.datetime & ":<br>" & ProMail.selected_mail.body)

  case "reply_all"
    MyForms.Screen("Std Composer").Component("subject").defaultValue("RE: " & ProMail.selected_mail.subject)
    if ProMail.selected_mail.sender.name = "" then
        sender_email = ProMail.selected_mail.sender.email
    else
        sender_email = ProMail.selected_mail.sender.name & " <" & ProMail.selected_mail.sender.email & ">"
    end if
    for each r in ProMail.selected_mail.recipients
        sender_email = sender_email & ", "
        if r.name = "" then
            sender_email = sender_email & r.email
        else
            sender_email = sender_email & r.name & " <" & r.email & ">"
        end if
    next
    MyForms.Screen("Std Composer").Component("toemail").defaultValue(sender_email)
    sender_name = ProMail.selected_mail.sender.name
    if sender_name = "" then
        sender_name = ProMail.selected_mail.sender.email
    end if
    MyForms.Screen("Std Composer").Component("message").defaultValue("<br><br>In reply to message from " & sender_name & " on " & ProMail.selected_mail.datetime & ":<br>" & ProMail.selected_mail.body)

  case "forward"
    MyForms.Screen("Std Composer").Component("subject").defaultValue("Fwd: " & ProMail.selected_mail.subject)
    sender_name = ProMail.selected_mail.sender.name
    if sender_name = "" then
        sender_name = ProMail.selected_mail.sender.email
    end if
    MyForms.Screen("Std Composer").Component("message").defaultValue("<br>Begin forwarded message from " & sender_name & " on " & ProMail.selected_mail.datetime & ":<br>" & ProMail.selected_mail.body)

end select

'-------------------------------------- Msg Email sent -----------------------------------------
' Msg box to show the email was sent
'-----------------------------------------------------------------------------------------------
    MyForms.addScreen("Email Sent Msg")
    MyForms.Screen("Email Sent Msg").addComponent("comment","Text")
    MyForms.Screen("Email Sent Msg").Component("comment").setCenter(true)
    MyForms.Screen("Email Sent Msg").Component("comment").value("<br>Your email was correctly sent.")
    MyForms.Screen("Email Sent Msg").Title = "Sending email ..."
    MyForms.Screen("Email Sent Msg").addComponent("timeout","Timer")
    MyForms.Screen("Email Sent Msg").Component("timeout").setTimer("Exit",1000)

    MyForms.addScreen("Email Send Err")
    MyForms.Screen("Email Send Err").addComponent("comment","Text")
    MyForms.Screen("Email Send Err").Component("comment").setCenter(true)
    MyForms.Screen("Email Send Err").Title = "Error"
    MyForms.Screen("Email Send Err").addComponent("btns","btngroup")
    MyForms.Screen("Email Send Err").Component("btns").addBtn("OK","Init")
    
     
    
 Function tt(mailbox)
 
 content = args("message")
 
   ' userguid = Proadmin.currentuser().guid
   ' logger(userguid)
    
    files = Dictionary
    logger("Attachments: " & args("attach"))
    if args("attach") <> "" then
        attach = split(args("attach"), ",")
        for each a in attach
'            logger("Attachment: " & a)
            Set f = xml_dialog.uploadedfile(a)
'            logger("- name: " & f.name)
            try
                files(f.name) = f.data
                f.remove()
            catch
            end try
        next
'        for each f in files
'            logger(f)
'        next
    end if
    xml_dialog.clearfiles()

    f = 0
    toEmails = Appinmail.utils.parseEmails(args)
    for each email in toEmails
'        try
            result = mailbox.send_email(email, "", "", args("subject"), content, files)
            if result = -1 then
                MyForms.Screen("Email Send Err").Component("comment").value("<br>An error occured while sending your email: " & mailbox.error_text)
                MyForms.ShowScreen("Email Send Err")
                exit function
            end if
            f = 1
'        catch
'        end try
    next
    if f = 1 then
        MyForms.ShowScreen("Email Sent Msg")  
    end if

end function 
'############################################################################################
'#                                                                                          #
'#                         State machine start with state / Init /                          #
'#                                                                                          #
'############################################################################################

if instr(operation_step,">")<>0 then
  mainStep = split(operation_step,">")(0)
  subStep = split(operation_step,">")(1)
else
  mainStep = operation_step
  subStep = ""
end if

logger ("operation step :" & operation_step)

Select case mainStep

  case "Init" 
    MyForms.ShowScreen("Std Composer")

  case "sendEmail"

  Call tt(ProMail.selected_mailbox)

  case "Exit"
    logger ("End of Processing Rules")

end select