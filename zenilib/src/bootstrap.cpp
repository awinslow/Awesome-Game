/* This file is part of the Zenipex Library (zenilib).
 * Copyleft (C) 2011 Mitchell Keith Bloch (bazald).
 *
 * This source file is simply under the public domain.
 */

#include <zenilib.h>

#if defined(_DEBUG) && defined(_WINDOWS)
#define DEBUG_NEW new(_NORMAL_BLOCK, __FILE__, __LINE__)
#define new DEBUG_NEW
#endif

using namespace std;
using namespace Zeni;

class Game_Object {
public:
  Game_Object(const Point2f &position_,
              const Vector2f &size_,
              const float &theta_ = 0.0f,
              const float &speed_ = 0.0f)
  : m_position(position_),
    m_size(size_),
    m_theta(theta_),
    m_speed(speed_)
  {
  }
 
  bool collide(const Game_Object &rhs) const {
    const Vector2f dist_vec = m_position - rhs.m_position +
                              0.5f * (m_size - rhs.m_size);
    const float dist = sqrt(dist_vec * dist_vec);
 
    return dist < get_radius() + rhs.get_radius();
  }

  virtual void render() const = 0; // pure virtual function call
  
  // If you might delete base class pointers, you need a virtual destructor.
  virtual ~Game_Object() {}

  void turn_left(const float &theta_) {
    m_theta += theta_;
  }
 
  void move_forward(const float &move_) {
    m_position.x += move_ * cos(m_theta);
    m_position.y += move_ * -sin(m_theta);
  }

    const Point2f & get_position() const {return m_position;}
  const Vector2f & get_size() const {return m_size;}
  const float & get_theta() const {return m_theta;}
 
  const float get_radius() const {
    return 0.5f * m_size.magnitude();
  }
 
private:
  Point2f m_position; // Upper left corner
  Vector2f m_size; // (width, height)
  float m_theta;
 
  float m_speed;

  public:

protected:
  void render(const String &texture, const Color &filter = Color()) const {
    // Use a helper defined in Zeni/EZ2D.h
    render_image(
      texture, // which texture to use
      m_position, // upper-left corner
      m_position + m_size, // lower-right corner
      m_theta, // rotation in radians
      1.0f, // scaling factor
      m_position + 0.5f * m_size, // point to rotate & scale about
      false, // whether or not to horizontally flip the texture
      filter); // what Color to "paint" the texture
  }
};

class Bullet : public Game_Object {
public:
  Bullet(const Point2f &position_,
         const Vector2f &size_,
         const float &theta_)
  : Game_Object(position_, size_, theta_)  {
  }
 
  void render() const {
    Game_Object::render("bullet");
  }
};

class Tank : public Game_Object {
public:
  Tank(const Point2f &position_,
       const Vector2f &size_,
       const float &theta_	   
	   )
  : Game_Object(position_, size_, theta_)
  {
	  m_exploded = false;
  }
  
  bool has_exploded() {return m_exploded;}
 
  void collide(const list<Bullet *> &bullets) {
    if(!m_exploded)
      for(list<Bullet *>::const_iterator it = bullets.begin(); it != bullets.end(); ++it)
        if((*it)->collide(*this)) {
          m_exploded = true;
          break;
        }
  }

  void render() const {
	  Game_Object::render("tank");
	 if(m_exploded)
      render_image("boom",
                   get_position(),
                   get_position() + get_size());
    
  }
  Bullet * fire() const {
    const float radius = 1.2f * get_radius();
    const Vector2f bullet_size(8.0f, 8.0f);
 
    const Point2f position(get_position() +
                           0.5f * (get_size() - bullet_size) +
                           radius * Vector2f(cos(get_theta()), -sin(get_theta())));
 
    return new Bullet(position, bullet_size, get_theta());
  }
private:
	bool m_exploded;
};

class Play_State : public Gamestate_Base {
  Play_State(const Play_State &);
  Play_State operator=(const Play_State &);
 
public:
  Play_State()
    : m_tank(Point2f(0.0f, 0.0f), Vector2f(64.0f, 64.0f), Global::pi * 1.5f),
    m_forward(false),
    m_turn_left(false),
    m_backward(false),
    m_turn_right(false), 
	m_time_passed(0.0f), 
	m_fire(false), 
	m_enemy(Point2f(400.0f, 400.0f), Vector2f(64.0f, 64.0f), Global::pi * 0.75f)
	, m_enemy_forward(false)
    , m_enemy_turn_left(false)
    , m_enemy_backward(false)
    , m_enemy_turn_right(false)
    , m_enemy_fire(false),
	m_prev_clear_color(get_Video().get_clear_Color())
  {
    set_pausable(true);
	m_chrono.start();
  }

    ~Play_State() {
    for(list<Bullet *>::iterator it = m_bullets.begin(); it != m_bullets.end(); ++it)
      delete *it;
  }
 
private:
	Chronometer<Time> m_explosion;
	Color m_prev_clear_color;
	  bool m_enemy_forward;
  bool m_enemy_turn_left;
  bool m_enemy_backward;
  bool m_enemy_turn_right;
  bool m_enemy_fire;
	Tank m_enemy;
	  bool m_fire;
  list<Bullet *> m_bullets;
  void on_push() {
    get_Window().mouse_hide(true);
	get_Video().set_clear_Color(Color(1.0f, 0.26f, 0.13f, 0.0f));
    //get_Window().mouse_grab(true);
  }
 
  void on_pop() {
    //get_Window().mouse_grab(false);
    get_Window().mouse_hide(false);
		get_Video().set_clear_Color(m_prev_clear_color);
  }

  Chronometer<Time> m_chrono;
  float m_time_passed;
 
  void perform_logic() {
    const float time_passed = m_chrono.seconds();
    const float time_step = time_passed - m_time_passed;
    m_time_passed = time_passed;
 if(!m_tank.has_exploded()) {
    // without a multiplier, this will rotate a full turn after ~6.28s
    m_tank.turn_left((m_turn_left - m_turn_right) * time_step);
    // without the '100.0f', it would move at ~1px/s
    m_tank.move_forward((m_forward - m_backward) * time_step * 100.0f);
	if(m_fire) {
      m_fire = false;
      m_bullets.push_back(m_tank.fire());
    }
 }
	    for(list<Bullet *>::iterator it = m_bullets.begin(); it != m_bullets.end(); ++it)
      (*it)->move_forward(time_step * 200.0f);
		    for(list<Bullet *>::iterator it = m_bullets.begin(); it != m_bullets.end();) {
      const Point2f &p = (*it)->get_position();
 
      if(p.x < -10.0f || p.x > 10.0f + get_Window().get_width() ||
         p.y < -10.0f || p.y > 10.0f + get_Window().get_height())
      {
        delete *it;
        it = m_bullets.erase(it);
      }
      else
        ++it;
    }
	m_tank.collide(m_bullets);
	m_enemy.collide(m_bullets);

	    if(!m_enemy.has_exploded()) {
      m_enemy.turn_left((m_enemy_turn_left - m_enemy_turn_right) * time_step);
      m_enemy.move_forward((m_enemy_forward - m_enemy_backward) * time_step * 100.0f);
 
      if(m_enemy_fire) {
        m_enemy_fire = false;
        m_bullets.push_back(m_enemy.fire());
      }
    }

		    if(m_tank.has_exploded() || m_enemy.has_exploded())
      if(m_explosion.is_running()) {
        if(m_explosion.seconds() > 3.0f)
          get_Game().pop_state();
      }
      else
        m_explosion.start();
  }

  void on_key(const SDL_KeyboardEvent &event) {
    switch(event.keysym.sym) {
      case SDLK_w:
        m_forward = event.type == SDL_KEYDOWN;
        break;
 
      case SDLK_a:
        m_turn_left = event.type == SDL_KEYDOWN;
        break;
 
      case SDLK_s:
        m_backward = event.type == SDL_KEYDOWN;
        break;
 
      case SDLK_d:
        m_turn_right = event.type == SDL_KEYDOWN;
        break;

	  case SDLK_SPACE:
        m_fire = event.type == SDL_KEYDOWN;
        break;

	  case SDLK_UP:
        m_enemy_forward = event.type == SDL_KEYDOWN;
        break;
 
      case SDLK_LEFT:
        m_enemy_turn_left = event.type == SDL_KEYDOWN;
        break;
 
      case SDLK_DOWN:
        m_enemy_backward = event.type == SDL_KEYDOWN;
        break;
 
      case SDLK_RIGHT:
        m_enemy_turn_right = event.type == SDL_KEYDOWN;
        break;
 
      case SDLK_RETURN:
        m_enemy_fire = event.type == SDL_KEYDOWN;
        break;
 
      default:
        Gamestate_Base::on_key(event); // Let Gamestate_Base handle it
        break;
    }
  }
 
  void render() {
    get_Video().set_2d();
 
    m_tank.render();
	    for(list<Bullet *>::const_iterator it = m_bullets.begin(); it != m_bullets.end(); ++it)
      (*it)->render();
	m_enemy.render();
  }
 
  Tank m_tank;
  bool m_forward;
  bool m_turn_left;
  bool m_backward;
  bool m_turn_right;
};


class Instructions_State : public Widget_Gamestate {
  Instructions_State(const Instructions_State &);
  Instructions_State operator=(const Instructions_State &);

public:
  Instructions_State()
    : Widget_Gamestate(make_pair(Point2f(0.0f, 0.0f), Point2f(800.0f, 600.0f)))
  {
  }

private:
  void on_key(const SDL_KeyboardEvent &event) {
    if(event.keysym.sym == SDLK_ESCAPE && event.state == SDL_PRESSED)
      get_Game().pop_state();
  }

  void render() {
    Widget_Gamestate::render();

    Zeni::Font &fr = get_Fonts()["title"];

    fr.render_text(
#if defined(_WINDOWS)
                   "ALT+F4"
#elif defined(_MACOSX)
                   "Apple+Q"
#else
                   "Ctrl+Q"
#endif
                           " to Quit",
                   Point2f(400.0f, 300.0f - 0.5f * fr.get_text_height()),
                   get_Colors()["title_text"],
                   ZENI_CENTER);
  }
};

class Title_State_Custom : public Title_State<Play_State, Instructions_State> {
public:
  Title_State_Custom()
    : Title_State<Play_State, Instructions_State>("")
  {
    m_widgets.unlend_Widget(title);
  }
 
  void render() {
    Title_State<Play_State, Instructions_State>::render();
 
    render_image("logo", Point2f(200.0f, 25.0f), Point2f(600.0f, 225.0f));
  }
};

class Bootstrap {
  class Gamestate_One_Initializer : public Gamestate_Zero_Initializer {
    virtual Gamestate_Base * operator()() {
      Window::set_title("zenilib Application");

      get_Joysticks();
      get_Video();
      get_Textures();
      get_Fonts();
      get_Sounds();
      get_Game().joy_mouse.enabled = true;

      return new Title_State_Custom;//Title_State<Play_State, Instructions_State>("Zenipex Library\nApplication");
    }
  } m_goi;

public:
  Bootstrap() {
    g_gzi = &m_goi;
  }
} g_bootstrap;

int main(int argc, char **argv) {
  return zenilib_main(argc, argv);
}
