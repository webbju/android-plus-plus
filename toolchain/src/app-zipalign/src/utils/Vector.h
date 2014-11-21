
#include <vector>

template <class T>
class Vector : public std::vector<T>
{
public:

  void add (const T& t)
  {
    push_back (t);
  }

  void removeAt (int i)
  {
    erase (begin () + i);
  }

};
