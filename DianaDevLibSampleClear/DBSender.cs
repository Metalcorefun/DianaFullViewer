using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Npgsql;

using static DianaDevLibSample.DianaDevLibDLL;

namespace DianaDevLibSample
{
    
    class DBSender
    {
        public int CurrentTestee { get; set; }
        private IList<Testee> testeesList = new List<Testee>(); 
        public IList<Testee> GetTesteesList()
        {
            return testeesList;
        }

        private IList<Gender> gendersList = new List<Gender>();
        public IList<Gender> GetGendersList()
        {
            return gendersList;
        }

        string connections = "Host=localhost;Database=DianaTest_2;Username=postgres;Password=hetfield1602year96;Persist Security Info=True";

        private string testeeInsertQuery = "Insert into Testee (surname, name, patronium, birth_date, id_gender) values (@surname, @name, @patronium, @birth_date, @id_gender)";
        private string testeeSelectQuery = "Select * from Testee";
        public void PopulateTesteesList()
        {
            testeesList.Clear();
            using (NpgsqlConnection connection = new NpgsqlConnection(connections))
            {
                using (NpgsqlCommand command = new NpgsqlCommand(testeeSelectQuery, connection))
                {
                    connection.Open();
                    NpgsqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Testee temp = new Testee
                            {
                                ID_testee = reader.GetInt16(0),
                                ID_gender = reader.GetInt16(1),
                                Surname = reader.GetString(2),
                                Name = reader.GetString(3),
                                Patronium = reader.GetString(4),
                                BirthDate = reader.GetDateTime(5)
                            };
                            testeesList.Add(temp);
                        }
                    }
                    reader.Close();
                }
            }
        }

        public void NewTestee(Testee temp)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connections))
            {
                connection.Open();
                using (NpgsqlTransaction transaction = connection.BeginTransaction())
                {
                    using (NpgsqlCommand command = new NpgsqlCommand(testeeInsertQuery, connection))
                    {
                        command.Transaction = transaction;
                        command.Parameters.Add("@surname", NpgsqlTypes.NpgsqlDbType.Varchar, 200);
                        command.Parameters.Add("@name", NpgsqlTypes.NpgsqlDbType.Varchar, 200);
                        command.Parameters.Add("@patronium", NpgsqlTypes.NpgsqlDbType.Varchar, 200);
                        command.Parameters.Add("@birth_date", NpgsqlTypes.NpgsqlDbType.Date);
                        command.Parameters.Add("@id_gender", NpgsqlTypes.NpgsqlDbType.Integer);

                        try
                        {
                            command.Parameters[0].Value = temp.Surname;
                            command.Parameters[1].Value = temp.Name;
                            command.Parameters[2].Value = temp.Patronium;
                            command.Parameters[3].Value = temp.BirthDate;
                            command.Parameters[4].Value = temp.ID_gender;
                            if (command.ExecuteNonQuery() != 1)
                            { throw new InvalidProgramException(); }
                            transaction.Commit();
                        }
                        catch (Exception) { transaction.Rollback(); }
                    }
                }
            }
        }

        private string genderSelectQuery = "Select * from Gender";
        public void PopulateGendersList()
        {
            gendersList.Clear();
            using (NpgsqlConnection connection = new NpgsqlConnection(connections))
            {
                using (NpgsqlCommand command = new NpgsqlCommand(genderSelectQuery, connection))
                {
                    connection.Open();
                    NpgsqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Gender temp = new Gender
                            {
                                ID_gender = reader.GetInt16(0),
                                Description = reader.GetString(1)
                            };
                            gendersList.Add(temp);
                        }
                    }
                    reader.Close();
                }
            }
        }

        private string questionInsertQuery = "Insert into Question (question_text) values (@question_text)";
        private string questionSelectQuery = "Select id_question from question where question_text = @question_text";
        private string answerInsertQuery = "Insert into Answer (id_question, id_testee, answer_grade) values (@id_question, @id_testee, @answer_grade)";
        public void SendQuestion(Question question)
        {
            if(CurrentTestee != -1)
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connections))
                {
                    int questionId = -1;
                    connection.Open();
                    using (NpgsqlTransaction transaction = connection.BeginTransaction())
                    {
                        using (NpgsqlCommand command = new NpgsqlCommand(questionInsertQuery, connection))
                        {
                            command.Transaction = transaction;
                            command.Parameters.Add("@question_text", NpgsqlTypes.NpgsqlDbType.Varchar, 200);
                            try
                            {
                                command.Parameters[0].Value = question.Text;
                                if (command.ExecuteNonQuery() != 1)
                                { throw new InvalidProgramException(); }

                                transaction.Commit();
                            }
                            catch (Exception) { transaction.Rollback(); }
                        }
                    }
                    using (NpgsqlTransaction transaction = connection.BeginTransaction())
                    {
                        using (NpgsqlCommand command = new NpgsqlCommand(questionSelectQuery, connection))
                        {
                            command.Transaction = transaction;
                            command.Parameters.Add("@question_text", NpgsqlTypes.NpgsqlDbType.Varchar, 200);
                            try
                            {
                                command.Parameters[0].Value = question.Text;

                                questionId = (int)command.ExecuteScalar();

                                transaction.Commit();
                            }
                            catch (Exception) { transaction.Rollback(); }
                        }
                    }
                    using (NpgsqlTransaction transaction = connection.BeginTransaction())
                    {
                        using (NpgsqlCommand command = new NpgsqlCommand(answerInsertQuery, connection))
                        {
                            command.Transaction = transaction;
                            command.Parameters.Add("@id_question", NpgsqlTypes.NpgsqlDbType.Integer);
                            command.Parameters.Add("@id_testee", NpgsqlTypes.NpgsqlDbType.Integer);
                            command.Parameters.Add("@answer_grade", NpgsqlTypes.NpgsqlDbType.Integer);
                            try
                            {
                                command.Parameters[0].Value = questionId;
                                command.Parameters[1].Value = CurrentTestee;
                                command.Parameters[2].Value = question.Answer;
                                if (command.ExecuteNonQuery() != 1)
                                { throw new InvalidProgramException(); }

                                transaction.Commit();
                            }
                            catch (Exception) { transaction.Rollback(); }
                        }
                    }
                }
            }
        }

        private String dataQuery = "Insert into Measurement (id_measurement_channel, measurement_value, measurement_timestamp) values (@id_measurement_channel, @measurement_value, @measurement_timestamp)";
        public void SendData(ushort[] DATA_PACKAGE)
        {
            if(CurrentTestee != -1)
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connections))
                {
                    connection.Open();
                    using (NpgsqlTransaction transaction = connection.BeginTransaction())
                    {
                        using (NpgsqlCommand command = new NpgsqlCommand(dataQuery, connection))
                        {
                            command.Transaction = transaction;
                            command.Parameters.Add("@id_measurement_channel", NpgsqlTypes.NpgsqlDbType.Integer);
                            command.Parameters.Add("@measurement_value", NpgsqlTypes.NpgsqlDbType.Smallint);
                            command.Parameters.Add("@measurement_timestamp", NpgsqlTypes.NpgsqlDbType.Timestamp);
                            int INDEX = 0;
                            try
                            {
                                foreach (ushort measure in DATA_PACKAGE)
                                {
                                    command.Parameters[0].Value = INDEX + 1;
                                    command.Parameters[1].Value = DATA_PACKAGE[INDEX];
                                    command.Parameters[2].Value = DateTime.Now;
                                    if (command.ExecuteNonQuery() != 1)
                                    { throw new InvalidProgramException(); }
                                    INDEX++;
                                }
                                transaction.Commit();
                            }
                            catch (Exception) { transaction.Rollback(); }
                        }
                    }
                }
            }
        }
    }

    public struct Gender
    {
        public int ID_gender { get; set; }
        public String Description { get; set; }
    }

    public struct Testee
    {
        public int ID_testee { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string Patronium { get; set; }
        public string FullName
        {
            get
            {
                return Surname + " " + Name + " " + Patronium;
            }
        }
        public DateTime BirthDate { get; set; }
        public int ID_gender { get; set; }
    }

    public struct Question
    {
        public string Text { get; set; }
        public int Answer { get; set; }
        public DateTime Timestamp { get; set; }

        public Question(string text, int answer, DateTime timestamp)
        {
            Text = text;
            Answer = answer;
            Timestamp = timestamp;
        }
    }
}
