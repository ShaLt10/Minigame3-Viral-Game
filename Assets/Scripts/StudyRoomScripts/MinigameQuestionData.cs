using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DigitalForensicsQuiz
{
    public enum QuestionType
    {
        MultipleChoice,
        DragAndDrop
    }

    [System.Serializable]
    public class MinigameQuestionData
    {
        [Header("Basic Question Info")]
        public string id;
        public QuestionType type;
        public string questionText;
        public string explanation;

        [Header("Multiple Choice Data")]
        public List<string> options = new List<string>();
        public int correctAnswerIndex;
        
        [Header("Drag and Drop Data")]
        public List<DragScenario> scenarios = new List<DragScenario>();
        public List<DropCategory> categories = new List<DropCategory>();
        public List<CorrectPair> correctPairs = new List<CorrectPair>();

        // Validation
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(questionText) || string.IsNullOrEmpty(explanation)) 
                return false;

            switch (type)
            {
                case QuestionType.MultipleChoice:
                    return options.Count >= 2 && 
                           correctAnswerIndex >= 0 && 
                           correctAnswerIndex < options.Count;
                           
                case QuestionType.DragAndDrop:
                    return scenarios.Count > 0 && 
                           categories.Count > 0 && 
                           correctPairs.Count > 0 &&
                           correctPairs.All(pair => 
                               scenarios.Any(s => s.id == pair.scenarioId) &&
                               categories.Any(c => c.id == pair.categoryId));
                default:
                    return false;
            }
        }

        // Get shuffled options for multiple choice
        public List<string> GetShuffledOptions(out int newCorrectIndex)
        {
            if (type != QuestionType.MultipleChoice)
            {
                newCorrectIndex = -1;
                return new List<string>();
            }

            var shuffledOptions = new List<string>(options);
            string correctAnswer = options[correctAnswerIndex];
            
            // Fisher-Yates shuffle
            for (int i = shuffledOptions.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                string temp = shuffledOptions[i];
                shuffledOptions[i] = shuffledOptions[j];
                shuffledOptions[j] = temp;
            }
            
            // Find new correct index
            newCorrectIndex = shuffledOptions.IndexOf(correctAnswer);
            return shuffledOptions;
        }

        // Get shuffled scenarios for drag and drop
        public List<DragScenario> GetShuffledScenarios()
        {
            if (type != QuestionType.DragAndDrop)
                return new List<DragScenario>();

            var shuffled = new List<DragScenario>(scenarios);
            
            // Fisher-Yates shuffle
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }
            
            return shuffled;
        }
    }

    [System.Serializable]
    public class DragScenario
    {
        public string id;
        public string description;
        public string detailedInfo; // For feedback in MinigameManager
        public Color backgroundColor = Color.white;
    }

    [System.Serializable]
    public class DropCategory
    {
        public string id;
        public string categoryName; // MISINFORMASI, MALINFORMASI, DISINFORMASI
        public Color zoneColor = Color.gray;
    }

    [System.Serializable]
    public class CorrectPair
    {
        public string scenarioId;
        public string categoryId;
        
        public CorrectPair(string scenarioId, string categoryId)
        {
            this.scenarioId = scenarioId;
            this.categoryId = categoryId;
        }
    }

    // ============================
    // QUESTION DATA PROVIDER
    // ============================
    
    public static class QuestionProvider
    {
        public static List<MinigameQuestionData> GetAllQuestions()
        {
            var questions = new List<MinigameQuestionData>();

            // Questions 1-5: Multiple Choice
            questions.Add(CreateMultipleChoiceQuestion1());
            questions.Add(CreateMultipleChoiceQuestion2());
            questions.Add(CreateMultipleChoiceQuestion3());
            questions.Add(CreateMultipleChoiceQuestion4());
            questions.Add(CreateMultipleChoiceQuestion5());
            
            // Question 6: Drag and Drop
            questions.Add(CreateDragAndDropQuestion());

            return questions;
        }

        static MinigameQuestionData CreateMultipleChoiceQuestion1()
        {
            return new MinigameQuestionData
            {
                id = "Q001",
                type = QuestionType.MultipleChoice,
                questionText = "Sebuah akun publik membagikan ulang cuplikan video lama saat seorang dokter mengatakan bahwa \"vaksin tertentu masih dalam uji coba dan belum final.\" Potongan ini dipakai untuk memperkuat narasi anti-vaksin. Jenis informasi apa yang paling tepat menggambarkan konten tersebut?",
                explanation = "Potongan informasi disengaja disebar ulang tanpa konteks waktu untuk menciptakan efek menyesatkan, padahal sumber awalnya benar. Ini adalah disinformasi karena ada niat untuk menyesatkan dengan menghilangkan konteks penting.",
                options = new List<string>
                {
                    "Misinformasi – kontennya tidak sengaja menyesatkan karena diambil dari sumber asli",
                    "Disinformasi – kontennya disengaja untuk menyesatkan, dengan menghilangkan konteks waktu",
                    "Malinformasi – informasinya benar, tapi dibagikan dengan tujuan edukatif",
                    "Disinformasi – kontennya salah dan tidak berasal dari sumber medis sama sekali"
                },
                correctAnswerIndex = 1
            };
        }

        static MinigameQuestionData CreateMultipleChoiceQuestion2()
        {
            return new MinigameQuestionData
            {
                id = "Q002",
                type = QuestionType.MultipleChoice,
                questionText = "Sebuah berita viral mengklaim bahwa \"air rebusan sereh dan jeruk nipis terbukti membunuh virus COVID-19.\" Kamu menemukan ada 3 sumber: (A) Akun blog kesehatan pribadi, (B) Portal berita nasional tanpa referensi ilmiah, (C) Artikel jurnal kedokteran internasional. Apa langkah terbaik sesuai SIFT?",
                explanation = "Dalam kerangka SIFT, langkah tepat adalah 'trace claim' ke sumber primer yang kredibel, yaitu jurnal kedokteran. Popularitas atau opini publik bukan indikator kebenaran ilmiah.",
                options = new List<string>
                {
                    "Bandingkan ketiga sumber, lalu pilih yang paling meyakinkan dari bahasanya",
                    "Menggunakan artikel yang paling banyak dishare sebagai yang paling sah",
                    "Melacak ke jurnal kedokteran untuk melihat apakah ada studi pendukung",
                    "Menanyakan ke grup WhatsApp dan pilih jawaban terbanyak"
                },
                correctAnswerIndex = 2
            };
        }

        static MinigameQuestionData CreateMultipleChoiceQuestion3()
        {
            return new MinigameQuestionData
            {
                id = "Q003",
                type = QuestionType.MultipleChoice,
                questionText = "Seseorang membocorkan data pribadi pejabat publik yang valid, termasuk alamat rumah dan nama anak, lalu menambahkan caption bernada provokatif: \"Biar rakyat tahu siapa yang sebenarnya korup. Bagikan sebanyak mungkin!\" Jenis manipulasi digital apa yang sedang terjadi?",
                explanation = "Malinformasi = informasi yang benar tapi disebar dengan niat jahat atau tanpa etika, seperti doxing atau memprovokasi. Ini yang paling berbahaya secara moral karena melanggar privasi dan keamanan.",
                options = new List<string>
                {
                    "Disinformasi – informasi palsu tentang pejabat disebarkan untuk menipu",
                    "Misinformasi – informasi benar tapi dibagikan secara keliru tanpa niat buruk",
                    "Malinformasi – informasi valid yang digunakan untuk menyakiti atau menyerang",
                    "Hoaks biasa – karena tidak ada unsur digital yang dimanipulasi"
                },
                correctAnswerIndex = 2
            };
        }

        static MinigameQuestionData CreateMultipleChoiceQuestion4()
        {
            return new MinigameQuestionData
            {
                id = "Q004",
                type = QuestionType.MultipleChoice,
                questionText = "Sebuah akun TikTok sering membagikan \"fakta-fakta mengejutkan\" seputar konspirasi dunia. Profilnya tidak memuat identitas, dan klaimnya tidak pernah menyertakan sumber. Namun, akun itu punya jutaan follower dan videonya sering FYP. Apa yang paling tepat kamu lakukan sebelum mempercayai isi kontennya?",
                explanation = "Langkah \"Investigate the source\" dari SIFT meminta kita melihat riwayat dan reputasi digital sang pembuat konten. Followers banyak bukan jaminan kebenaran. Kredibilitas harus dievaluasi berdasarkan transparansi dan akurasi historis.",
                options = new List<string>
                {
                    "Langsung berhenti menonton karena pasti hoaks",
                    "Telusuri kredibilitas akun dan cek apakah pernah dikoreksi oleh pemeriksa fakta",
                    "Bagikan dulu, lalu klarifikasi kalau ternyata salah",
                    "Anggap semua konspirasi menarik dan layak dipercaya sebagian"
                },
                correctAnswerIndex = 1
            };
        }

        static MinigameQuestionData CreateMultipleChoiceQuestion5()
        {
            return new MinigameQuestionData
            {
                id = "Q005",
                type = QuestionType.MultipleChoice,
                questionText = "Sebuah meme menyebar luas berisi gambar seseorang yang tampak mabuk di jalanan, dengan caption: \"Beginilah kalau generasi milenial jadi pemimpin.\" Kamu melacak gambar tersebut ternyata dari video lawas tahun 2015 yang tidak ada kaitannya dengan politik. Mengapa menyebarkan meme seperti ini sangat berbahaya?",
                explanation = "Meme disinformasi visual seperti ini menggunakan gambar nyata dengan framing menyesatkan, yang bisa mencemarkan nama baik dan memperkuat stigma palsu. Ini melanggar etika digital karena mencampur fakta dan opini tanpa konteks yang tepat.",
                options = new List<string>
                {
                    "Karena bisa menurunkan elektabilitas generasi muda",
                    "Karena kontennya lucu dan bisa disalahartikan sebagai humor",
                    "Karena mencampur fakta dan opini tanpa konteks melanggar etika digital",
                    "Karena tidak menyebutkan sumber gambar dan tidak menyebut tahun"
                },
                correctAnswerIndex = 2
            };
        }

        static MinigameQuestionData CreateDragAndDropQuestion()
        {
            return new MinigameQuestionData
            {
                id = "Q006",
                type = QuestionType.DragAndDrop,
                questionText = "Identifikasi jenis manipulasi informasi dengan menyeret setiap skenario ke kategori yang benar.",
                explanation = "DISINFORMASI = informasi palsu yang sengaja disebarkan untuk menipu. " +
                             "MISINFORMASI = informasi salah yang disebarkan tanpa niat jahat. " +
                             "MALINFORMASI = informasi benar yang disebarkan dengan niat jahat atau tanpa etika.",
                
                scenarios = new List<DragScenario>
                {
                    new DragScenario
                    {
                        id = "scenario_A",
                        description = "Scammer sengaja menyamar sebagai @genz.berdampak",
                        detailedInfo = "Penyamaran identitas dengan tujuan menipu - ini adalah DISINFORMASI karena informasi palsu disebarkan dengan sengaja untuk menipu korban.",
                        backgroundColor = new Color(0.9f, 0.9f, 1f, 1f)
                    },
                    new DragScenario
                    {
                        id = "scenario_B", 
                        description = "Aluna reshare tanpa cek keaslian",
                        detailedInfo = "Penyebaran informasi salah tanpa verifikasi - ini adalah MISINFORMASI karena informasi salah disebarkan tanpa niat jahat, hanya kurang teliti.",
                        backgroundColor = new Color(1f, 0.9f, 0.9f, 1f)
                    },
                    new DragScenario
                    {
                        id = "scenario_C",
                        description = "Kriminal mengancam menyalahgunakan data KTP asli Mbak Dyta",
                        detailedInfo = "Penggunaan data valid untuk tujuan jahat - ini adalah MALINFORMASI karena informasi benar (data KTP asli) digunakan dengan niat jahat untuk mengancam.",
                        backgroundColor = new Color(0.9f, 1f, 0.9f, 1f)
                    }
                },
                
                categories = new List<DropCategory>
                {
                    new DropCategory
                    {
                        id = "cat_disinformasi",
                        categoryName = "DISINFORMASI",
                        zoneColor = new Color(1f, 0.2f, 0.2f, 0.3f)
                    },
                    new DropCategory
                    {
                        id = "cat_misinformasi",
                        categoryName = "MISINFORMASI", 
                        zoneColor = new Color(1f, 0.8f, 0.2f, 0.3f)
                    },
                    new DropCategory
                    {
                        id = "cat_malinformasi",
                        categoryName = "MALINFORMASI",
                        zoneColor = new Color(0.8f, 0.2f, 1f, 0.3f)
                    }
                },
                
                correctPairs = new List<CorrectPair>
                {
                    new CorrectPair("scenario_A", "cat_disinformasi"),
                    new CorrectPair("scenario_B", "cat_misinformasi"),
                    new CorrectPair("scenario_C", "cat_malinformasi")
                }
            };
        }

        public static MinigameQuestionData GetQuestionById(string id)
        {
            var questions = GetAllQuestions();
            return questions.Find(q => q.id == id);
        }

        public static List<MinigameQuestionData> GetQuestionsByType(QuestionType type)
        {
            var questions = GetAllQuestions();
            return questions.FindAll(q => q.type == type);
        }

        public static bool ValidateAllQuestions()
        {
            var questions = GetAllQuestions();
            return questions.All(q => q.IsValid());
        }
    }

    // ============================
    // ANSWER VALIDATION
    // ============================

    public static class AnswerValidator
    {
        public static bool ValidateMultipleChoice(MinigameQuestionData question, int selectedAnswer)
        {
            if (question.type != QuestionType.MultipleChoice) return false;
            return selectedAnswer == question.correctAnswerIndex;
        }

        public static bool ValidateDragAndDrop(MinigameQuestionData question, Dictionary<string, string> playerAnswers)
        {
            if (question.type != QuestionType.DragAndDrop) return false;
            if (playerAnswers.Count != question.correctPairs.Count) return false;

            foreach (var correctPair in question.correctPairs)
            {
                if (!playerAnswers.ContainsKey(correctPair.scenarioId)) return false;
                if (playerAnswers[correctPair.scenarioId] != correctPair.categoryId) return false;
            }

            return true;
        }

        public static float GetPartialScore(MinigameQuestionData question, Dictionary<string, string> playerAnswers)
        {
            if (question.type != QuestionType.DragAndDrop) return 0f;
            
            int correctCount = 0;
            foreach (var correctPair in question.correctPairs)
            {
                if (playerAnswers.ContainsKey(correctPair.scenarioId) && 
                    playerAnswers[correctPair.scenarioId] == correctPair.categoryId)
                {
                    correctCount++;
                }
            }

            return (float)correctCount / question.correctPairs.Count;
        }
    }

    // ============================
    // GAME PROGRESS TRACKING
    // ============================

    [System.Serializable]
    public class GameProgress
    {
        public int currentQuestionIndex = 0;
        public List<bool> questionResults = new List<bool>(); // Track correct/incorrect for tracking bar
        public Dictionary<string, int> multipleChoiceAnswers = new Dictionary<string, int>();
        public Dictionary<string, Dictionary<string, string>> dragDropAnswers = new Dictionary<string, Dictionary<string, string>>();
        public float totalTime = 0f;
        public bool isCompleted = false;

        public int CorrectAnswersCount => questionResults.Count(result => result);
        public int TotalAnsweredQuestions => questionResults.Count;
        public float AccuracyPercentage => TotalAnsweredQuestions > 0 ? 
            (float)CorrectAnswersCount / TotalAnsweredQuestions * 100f : 0f;
    }

    // ============================
    // SIMPLE SAVE SYSTEM
    // ============================

    public static class SimpleSaveSystem
    {
        private const string PROGRESS_KEY = "GameProgress";

        public static void SaveProgress(GameProgress progress)
        {
            try
            {
                string json = JsonUtility.ToJson(progress);
                PlayerPrefs.SetString(PROGRESS_KEY, json);
                PlayerPrefs.Save();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save progress: {e.Message}");
            }
        }

        public static GameProgress LoadProgress()
        {
            try
            {
                if (!PlayerPrefs.HasKey(PROGRESS_KEY))
                    return new GameProgress();

                string json = PlayerPrefs.GetString(PROGRESS_KEY);
                return JsonUtility.FromJson<GameProgress>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load progress: {e.Message}");
                return new GameProgress();
            }
        }

        public static void ClearProgress()
        {
            PlayerPrefs.DeleteKey(PROGRESS_KEY);
            PlayerPrefs.Save();
        }

        public static bool HasSavedProgress()
        {
            return PlayerPrefs.HasKey(PROGRESS_KEY);
        }
    }

    // ============================
    // UTILITY CLASSES
    // ============================

    public static class PlatformUtils
    {
        public static bool IsAndroid()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            return true;
            #else
            return false;
            #endif
        }

        public static bool IsEditor()
        {
            #if UNITY_EDITOR
            return true;
            #else
            return false;
            #endif
        }

        // Auto-configure based on platform
        public static void ConfigureForPlatform()
        {
            if (IsAndroid())
            {
                // Android-specific configurations
                Application.targetFrameRate = 60;
                QualitySettings.vSyncCount = 0;
            }
            else if (IsEditor())
            {
                // Editor configurations for testing
                Application.targetFrameRate = -1; // Unlimited
            }
        }

        // Touch-friendly sizing
        public static Vector2 GetTouchFriendlySize(Vector2 baseSize)
        {
            if (IsAndroid())
            {
                float screenScale = Mathf.Min(Screen.width, Screen.height) / 1080f;
                return baseSize * Mathf.Clamp(screenScale, 0.8f, 1.5f);
            }
            return baseSize;
        }
    }
}