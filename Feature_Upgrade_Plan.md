I did a demo of this in its current state and recieved a lot of feedback of things that need tweaked or changed. Please analyze these and update the development plan accordingly. If there are questions or ambiguities in how any of these changes should be implemented please ask.

## Order Form

# General:
- All of the dropdowns need to have a uniform UI. Every dropdown should have the same look and feel as the Cake Tier Size, Cake Shape, and Cupcake Size dropdowns.
- Bug: When you click an order from elsewhere in the application (All orders, calendar, etc), and then click Order Form in the top nav to get to a blank new order, another order is loaded.
- Autosave was requested. As fields are entered, the order should be autosaved. I think this would require ignoring required fields, but i'm not sure how it would be implemented. In that scenario, they will need flagged as incomplete.
- Any orders that were autosaved/abandoned with required fields left empty should be flagged. On the homepage/overview, add a new section for Incomplete Orders. Link to the All Orders page, filtered by Incomplete. if any incomplete orders exist, make the button solid red. Otherwise, leave it white with blue text like the other buttons. 
- Change the wording of the Delete button to Archive. Similarly, throughout the application, there should be no UI element that mentions "Delete". While "Delete" was already a soft-delete, we want to expose this soft deletion as Archive. For maintainability, change backend references of Delete to Archive as well.
- When the user clicks the Archive button, before marking it as archived, show a modal with a large text box titled "Cancellation Reason", and Archive/Cancel buttons. It is a required free text field to continue with Archival. When they click Archive, save this Cancellation Reason and a timestamp alongside the order and mark it as archived, if they click cancel close the modal and do nothing.
- If the user opens an archived order, at the top of the screen between "Order Form" and the Title, display the Cancellation Reason in large font in an editable textbox with a thick red outline (matching the style of the Archive button, but not actually a button). 
- If the user opens an archived order, show a "Restore" button at the bottom alongside the Update Order button. When clicked, copy the Cancellation Reason into the Details. Place it at the top formatted "Original Cancellation Reason - [timestamp] - [Cancellation Reason Text]"

# Adding Cakes:

- A better implementation for half and half cakes needs made. There is a toggle, but no options for selecting two flavors of cake. 
- Its rare, but sometimes each layer of cake in a tier is customized to be different. An option for multiple tier flavors and inputs for them needs to be made.

For both of the above, the options for selecting further details should unhide/hide fields for taking those inputs so the UI isn't cluttered with all options.

- Each cake that is added to the order should be labelled so they can be easily identified. Next to the +/- buttons put a large A, B, C, etc label.
- When any cake is added, automatically add a new section to the notes field so details can immediately be typed in. Something as simple as "Cake A - " with a line break above it would work fine.

# Adding Cupcakes:

- There doesn't need to be a Signature dropdown when Custom Cupcakes is clicked. The field can be fully removed or hidden in this case.

# Adding Cookies:

- Cookies also need a dropdown for size, configurable via Management like other dropdowns. Seed default data as "standard" and "small".

# Order Information:
- There should be a 3rd radio button "Tasting" alongside Pickup and Delivery. 
- The Delivery/Pickup Date and Time should be split into two fields, one for Date and one for Time. Combining them both into the existing DateTime object is fine, they don't need stored as two separate datetimes. Use the existing DatePicker for Date, and a TimePicker for the time. 
- As part of splitting the above field, reorganize the form into 3 columns:
| Order Number | Customer Name | Details |
| Customer Email | Customer Phone Number | Details |
| Delivery Date | Delivery Time | Details |
| Delivery Location | Initial Contact Method | Details |
| Day-of/Secondary Contact Name | Day-of/Secondary Phone Number | Details |

- Then under this grid place the 3 radio buttons
- Under the radio buttons, add the 3 toggles. Also add a 4th toggle Completed. This will auto-toggle when the Delivery datetime has passed. The user can also toggle it to completed early. In the backend this can be the same as the Active/Inactive flag
- Move the "Add to order:" section and all of its buttons under the Order Information Section
- Before the Order Information section, add a new field "Title" with a textbox next to it. "Title" and the textbox should both be on the same line with the same font size as other subheaders like "Add to order" or "Order Information", as well as a horizontal rule underneath it

# Attachments:
- Bug: The Attachments section does not appear when the page is loaded fresh to enter a new order
- When an uploaded attachment is clicked on, it should open up in a modal that displays the image larger. If there are multiple images, that modal should be a carosel of all attachments.

# Add to order:
- There should be an "Other" button. It will have two free text fields, Name and Item. 

# Pricing:
- New fields need added: Labor, Flavor Upgrade, Lookbook Price.
- Add a general note to get greater details about pricing before starting on this section. Configuration and other fields will need added to auto-calculate price as information is entered.

## All Orders:
- The table is not sortable. All columns should be sortable.
- At the top of the page, add a toggle for Active/Incomplete/Inactive/Archived, with the same UI as the month/week toggle on the calendar page.
- As an extra indicator, make Incomplete red if any incomplete orders exist
- On page load, default to the list filtered by Active only. The only exception is when the page is loaded from the Incomplete button on the overview page
- Automatically mark orders as inactive once the order date has passed
- Add a Date column. When the page is loaded from anywhere in the app, the default table sort should be Date soonest first.

## Calendar:

The calendar page itself isn't needed, and can be removed. However, the Google Calendar integration is still part of the development plan. Events will be created and sent to Google Calendar. There's no need to reinvent the wheel and implement our own calendar here or frame google calendar on this site when they can just open it in a new tab.

The calendar event criteria are:
- Each event's title must be formatted "OrderNumber - Title - Name", where Title is from the new field added to the top of the order form, and Name is formatted "First Last".
- Events are colored using the following key:
  - Blue = cakes
  - Purple = cupcakes
  - green = completed/ready for pickup
  - red = cancelled
  - yellow = delivery
- Events are not to appear on the calendar at the given time. They will appear on the calender in the order they are entered. The first order will be at midnight. Each event is 30 minutes long, one after the other.
- Party Room Rentals (to be detailed later) will appear at their scheduled time. 

## Bake Sheet:

- The "Orders this week" collapsable section can be fully removed, it is not needed.
- Cakes and cupcakes should be combined. Interleave the two, grouped by cake flavor.
- Each layer of cake should be its own line. The current use/concept of the QTY column doesn't need to exist for this table.
- Each order of cupcakes should be its own line. For example if order 1234 ordes 12 chocolate cupckakes, and order 7890 orders 12 chocolate cupcakes, there should be a row for each order.
- The columns should be (in this order) Order Number, Size/Quantity, Flavor, Day of Week, Complete/Notes.
  - Order number is self explanitory. With each layer on its own line, this will have duplicate data (aka if order 1234 has one cake with 3 layers of chocolate, there will be 3 lines stating 1234, Choclolate)
  - Size/Quantity will have either the size of the cake layer, or the quantity of cupcakes of that flavor for the order. 
  - Flavor is unchanged
  - Day of Week is the name of the day of the week
  - Complete/Notes is a free text field that can be typed into. No idea where in the backend this value should be saved
- Days of the week cells have light background highlighting following this pattern:
  - Monday = orange
  - Tuesday = yellow
  - Wed = green
  - Thurs = blue
  - Friday = purple
  - Saturday - ?
- Using random colors (and hues different to the day of week highlights), highlight matching order numbers with a color. For example, if there are 3 lines with order number 1234, apply a matching light background color to these 3 cells. Then, if there are 2 lines with order number 7890, apply a new color to these two cells. Orders with only one line can be default/white. 
- For any Micro cakes, display the Size as 6", and write "Micro" in the Complete/Notes field
- For any quarter sheet, display the size as half sheet and write "Cut" in the Complete/Notes field
- For any quarter sheet that is also half and half cake flavor, display the size as half sheet and write "Cut twice" in the Complete/Notes field.
- The computation time (or "bake week") should start on Thursday and end on Wednesday. 
- Any order put in via the Other button should be listed at the bottom.
- The page should be redesigned to be as compact as possible, ready to be printed on an 8.5x11 page. 
- The cakes grid should show gridlines, as if it was a spreadsheet
- The content should be 100% width when being printed. Currently there are thick margins around the table in the print preview. Also only the Cakes table, Cupcakes, Cookies, and Pupcakes information should be printed. Currently the nav bar and other page content is printed.

## Management
- Deleted orders can be removed. As stated elsewhere, there will not be deleted orders, and archived/etc orders can be managed from the All Orders list.
- Party rental managment items will need added similar to other pages. These items are detailed below

## Party Rentals
- A new page to book party rentals will be added. It will have the following fields in a Booking Info section:
  - Name
  - Phone
  - Email
  - Type (management editable dropdown, seed data)
  - Date of Event
  - Time of Rental (start time and end time, probably two time pickers)
  - Number of Guests
  - Room arrangement (A set of images that can be changed via management will appear and be able to be selected like a big radio button)
- It will have the following fields in a Cost section:
  - Base Rental Rate (this value will be configurable via management and prefilled on page load)
  - Additional Hours (format as "[rate] x [blank field] hours)", where rate is configurable via management and the blank field takes a max 2 digit number)
- It will have the ability to add Add Ons, in a similar manner to adding a Cookie etc. to the Order Form
  - Add Ons will have a configurable drop down in management
  - When the item is selected, fields in that row will appear, Price and Notes
  - Price is predetermined and configured with the add on in management
  - Notes is a free text fields that will be saved with the party rental

A wishlist feature would be to auto-fill out a form that already exists. The current workflow is:
 - A master template (Google Doc) of this form is copied and saved in another folder
 - The copied form is filled out (all of the above fields)
 - It is renamed to their Name and Date
 - It is sent to them to digitally sign using the Google Workspace tools for that

It would be great, if possible, for a user to fill out these fields on this Party Rentals page, and the application copies, renames, and fills out the form. Is this possible?

## Site Functionality/Login

We think there should be a dual login screen system. If there is a better way of doing this, please suggest one.

The though is for there to be an initial login page. This would be a regular login screen with auth and security, etc, like any other website. For now, there will be no auth levels, likely there will only be one actual user. This is just to put a strong password screen in front of the interface.

One logged in, there would be a secondary login screen for each user. This would be a non-secure screen to enter a 4 digit pin. This would "log" this user in to the session under their account.

On a set timeout (a few minutes), the user would be soft-logged out back to the pin screen. From here they, or someone else, can enter a pin and be back in the system.

The thought process for this is to add an audit trail to all modifications to data within the system. In the database, audit tables would be created that showed which user was logged in, what was changed, and when it was changed. This would both let us see any malicious activity, or let us find small errors in data entry to roll them back.

For regular use, the user will essentially never be logged out of the main login screen, similar to logging into modern web apps like Youtube, Facebook, etc. I'd like there to be a cookie or auth token saved on the device so that when at the bakery, they are able to fast access features on the site. I'm not very familiar with building an auth system so let me know what is feasible or needs revision with this plan.

## Conclusion

There are a fair amount of usability features outlined here that will need addressed before considering deployment and hosting. Redo all of the incomplete phases of the Development Plan with these in mind. Determine which features can be done independantly as their own phase, and what needs done simultaniously with shared features/data. 

A few notes on the current phase 9:
 - Move Mobile optimization to its own phase, after all features, and before deployment/hosting
 - Remove the Notifications and Order status tracking items, they are not in scope/needed