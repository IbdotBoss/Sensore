# Sensore

A medical pressure monitoring system for pressure ulcer prevention.

## Tech Stack

- ASP.NET Core 9.0 (MVC + Razor Pages)
- Entity Framework Core 9.0
- SQL Server (LocalDB)
- Bootstrap 5, Chart.js

## Setup

1. Clone the repository:

2. Open the solution in Visual Studio 2022

3. Create the database (Package Manager Console):
   ```
   Add-Migration InitialCreate
   Update-Database
   ```

4. Run the application (F5 or Ctrl+F5)


## Login Credentials

| Role			| Email						 | Password       |
|---------------|----------------------------|----------------|
| **Admin**     | `admin@sensore.com`        | `Admin123!`    |
| **Admin**     | `ndubuisi@sensore.com`     | `Ndubuisi@123` |
| **Clinician** | `dr.smith@sensore.com`     | `Doctor@123`   |
| **Clinician** | `dr.ibrahim@sensore.com`   | `Ibrahim@123`  |
| **Patient**   | `zarah.haroon@sensore.com` | `Zarah@123`    |
| **Patient**   | `bruce.wayne@sensore.com`  | `Bruce@123`    |

## User Roles

| Role      | Permissions											  |
|-----------|---------------------------------------------------------|
| Admin     | Manage users, assign patients to clinicians			  |
| Clinician | View assigned patients, monitor data, adjust thresholds |
| Patient   | View personal dashboard, communicate with care team     |

## Features

- Real-time pressure heatmap (32x32 sensor grid)
- Pressure trend charts
- Configurable alert thresholds
- Patient-clinician messaging
- Risk score calculation
