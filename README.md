# ⛪ Church Program & Attendance App  

![License](https://img.shields.io/badge/license-MIT-green)  
![Build](https://img.shields.io/badge/build-passing-brightgreen)  
![Status](https://img.shields.io/badge/status-MVP-blue)  

A simple **web & mobile-based** application that allows church admins to create programs, automatically generate QR codes, and track both **member interest** and **attendance** in real time.  

---

## 📌 Features  

### **Admin (Backoffice)**  
- 🔑 Secure login for church staff.  
- 🗓 Create and manage church programs/events.  
- 📷 Auto-generate unique QR codes for each event.  
- 👥 View members who show interest before an event.  
- ✅ Track attendance by QR scans during the event.  
- 📤 Export attendance data (CSV/Excel).  

### **Members**  
- 📅 View upcoming church programs.  
- 📱 Scan QR code to show interest.  
- ⏳ Scan QR code to mark attendance on event day.  
- 📩 Instant confirmation after scanning.  

### **QR Code Logic**  
- **Interest QR Code** → Can be scanned anytime before the event date.  
- **Attendance QR Code** → Can only be scanned during the event time.  

---

## 🛠 Tech Stack (MVP Suggestion)  
- **Frontend:** React / Vue / Flutter (mobile)  
- **Backend:** Node.js (Express) / Laravel / Django  
- **Database:** MySQL / PostgreSQL / MongoDB  
- **QR Generation:** QR Code library (e.g., `qrcode` for Node.js)  
- **Auth:** JWT-based authentication  
- **Hosting:** Vercel / Heroku / AWS / Render  

---

## 📊 Data Models (Core Entities)  
- **Admin** – Event creators and managers.  
- **Member** – Church members (optional registration in MVP).  
- **Program** – Church programs/events with QR codes.  
- **Interest** – Members who show interest before events.  
- **Attendance** – Members who attend events.  

---

## 🚀 How It Works  
1. **Admin** creates a church program from the backoffice dashboard.  
2. The system **generates unique QR codes** for interest and attendance.  
3. Members **scan interest QR** before the event to register interest.  
4. Members **scan attendance QR** on the event day to mark attendance.  
5. **Admin** views participation stats in the dashboard.  

---

## 📂 Installation & Setup  

```bash
# Clone repository
git clone https://github.com/yourusername/church-attendance-app.git

# Navigate into folder
cd church-attendance-app

# Install dependencies
npm install   # or yarn install

# Start development server
npm run dev
