/*
Hoi Julian!
Hierbij mijn feedback op je code. Het ziet er gewoon prima uit! 
Ik heb het benaderd alsof ik een collega zou zijn, die na vakantie terug komt :)

- Summaries en Comments bij methods :)

- Seperation of concern: elke methode z'n eigen taak. 
  Bijvoorbeeld: LoginForm.btnLogin_Click() en MainForm.BtnAddProduct_Click():
  > Alle uitvoerende logica in hun eigen methodes
  > Laat eventhandlers _Click() die methodes aanroepen

- Grote methodes zoals MainForm.BuildLayout() zouden in kleinere methodes kunnen worden opgesplitst.

- Je zou #region - #endregion voor indelen kunnen toepassen.

- MainForm.Designer is aangepast en nu lijkt hardcoded te zijn in MainForm. Dit is natuurlijk prima, maar niet gebruikelijk. Waarom op deze manier?
  LoginForm gebruikt namelijk wel de standaard Designer.

- De strings "https://tsjjqscjfkicpoijuyur.supabase.co" en "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InRzampxc2NqZmtpY3BvaWp1eXVyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzgxNTQ2MzYsImV4cCI6MjA5MzczMDYzNn0.JU9ARRBdDmhPOtx4g61QGB4pD1uP0IilpiZOhnhJ61M" worden meerdere malen hardcoded gebruikt. 
  Zie ik het goed dat je dit door class SupabaseAuthService te implementeren wil voorkomen?

- In het kader van voorkomen van magic numbers: je zou Enum voor de UserRole 1, 2 en 3 toepassen:
  public enum UserRole
  {
      Admin = 1,
      Superuser = 2,
      Guest = 3
  }
*/